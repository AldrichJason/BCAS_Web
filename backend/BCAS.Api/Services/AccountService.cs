using BCAS.Api.Helpers;
using BCAS.Api.Models;
using BCAS.Api.Models.DTOs;
using BCAS.Api.Options;
using BCAS.Api.Repositories;
using BCAS.Api.Services;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BCAS.Api.Services;

public sealed class AccountService : IAccountService
{
    /// <summary>An invitation is given longer than a reset: the recipient may not be at their desk.</summary>
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    private readonly IAccountRepository _accounts;
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IActivityLogger _activityLog;
    private readonly IEmailSender _email;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppOptions _appOptions;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IAccountRepository accounts,
        IPasswordResetTokenRepository tokens,
        IPasswordHasher passwordHasher,
        IActivityLogger activityLog,
        IEmailSender email,
        IHttpContextAccessor httpContextAccessor,
        IOptions<AppOptions> appOptions,
        ILogger<AccountService> logger)
    {
        _accounts = accounts;
        _tokens = tokens;
        _passwordHasher = passwordHasher;
        _activityLog = activityLog;
        _email = email;
        _httpContextAccessor = httpContextAccessor;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken = default) =>
        _accounts.ListAsync(cancellationToken);

    public async Task<AccountReference> GetReferenceAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _accounts.ListRolesAsync(cancellationToken).ConfigureAwait(false);
        var departments = await _accounts.ListDepartmentsAsync(cancellationToken).ConfigureAwait(false);
        return new AccountReference(roles, departments);
    }

    public async Task<CreateUserResult> CreateAsync(
        CreateUserRequest request, int createdByUserId, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        request.Email = email;
        request.FirstName = request.FirstName.Trim();
        request.LastName = request.LastName.Trim();

        if (!RoleCodes.All.Contains(request.RoleCode))
        {
            return new CreateUserResult(CreateUserOutcome.UnknownRole, Message: "Choose one of the listed roles.");
        }

        // Department scope belongs to the Academic Head and to nobody else.
        if (RoleCodes.RequiresDepartment(request.RoleCode))
        {
            if (request.DepartmentId is not { } departmentId)
            {
                return new CreateUserResult(
                    CreateUserOutcome.InvalidDepartment, Message: "An Academic Head must be assigned a department.");
            }

            if (!await _accounts.DepartmentExistsAsync(departmentId, cancellationToken).ConfigureAwait(false))
            {
                return new CreateUserResult(
                    CreateUserOutcome.InvalidDepartment, Message: "Choose one of the listed departments.");
            }
        }
        else if (request.DepartmentId is not null)
        {
            return new CreateUserResult(
                CreateUserOutcome.InvalidDepartment,
                Message: "Only an Academic Head is scoped to a department. Leave it unset for this role.");
        }

        if (await _accounts.EmailExistsAsync(email, cancellationToken).ConfigureAwait(false))
        {
            return new CreateUserResult(
                CreateUserOutcome.DuplicateEmail, Message: $"An account already uses {email}.");
        }

        if (await _accounts.FindRoleIdAsync(request.RoleCode, cancellationToken).ConfigureAwait(false) is not { } roleId)
        {
            return new CreateUserResult(CreateUserOutcome.UnknownRole, Message: "Choose one of the listed roles.");
        }

        var newUserId = await _accounts
            .CreateAsync(request, roleId, UnusablePasswordHash(), createdByUserId, cancellationToken)
            .ConfigureAwait(false);

        await SendInvitationAsync(newUserId, email, request.FirstName, cancellationToken).ConfigureAwait(false);

        await _activityLog.LogAsync(
            "AccountProvisioned", createdByUserId, "User", Id(newUserId),
            $"{email} as {request.RoleCode}", cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "User {ActorId} provisioned account {UserId} with role {RoleCode}.",
            createdByUserId, newUserId, request.RoleCode);

        var account = await _accounts.FindAsync(newUserId, cancellationToken).ConfigureAwait(false);
        return new CreateUserResult(CreateUserOutcome.Created, account);
    }

    public async Task<SetActivationResult> SetActivationAsync(
        int userId, bool isActive, int changedByUserId, CancellationToken cancellationToken = default)
    {
        var account = await _accounts.FindAsync(userId, cancellationToken).ConfigureAwait(false);
        if (account is null)
        {
            return new SetActivationResult(SetActivationOutcome.NotFound);
        }

        if (!isActive)
        {
            if (userId == changedByUserId)
            {
                return new SetActivationResult(SetActivationOutcome.CannotDeactivateSelf, account);
            }

            // Refuse to leave the portal with no way back in.
            if (account.IsActive
                && string.Equals(account.RoleCode, RoleCodes.SuperAdmin, StringComparison.Ordinal)
                && await _accounts.CountActiveSuperAdminsAsync(cancellationToken).ConfigureAwait(false) <= 1)
            {
                return new SetActivationResult(SetActivationOutcome.LastActiveSuperAdmin, account);
            }
        }

        // Already in the requested state: nothing to write, and reporting success
        // keeps the toggle idempotent.
        if (account.IsActive == isActive)
        {
            return new SetActivationResult(SetActivationOutcome.Updated, account);
        }

        await _accounts.SetActivationAsync(userId, isActive, changedByUserId, cancellationToken).ConfigureAwait(false);

        await _activityLog.LogAsync(
            isActive ? "AccountActivated" : "AccountDeactivated",
            changedByUserId, "User", Id(userId), account.Email, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "User {ActorId} {Action} account {UserId}.",
            changedByUserId, isActive ? "activated" : "deactivated", userId);

        var updated = await _accounts.FindAsync(userId, cancellationToken).ConfigureAwait(false);
        return new SetActivationResult(SetActivationOutcome.Updated, updated);
    }

    // --- helpers -------------------------------------------------------------

    private async Task SendInvitationAsync(
        int userId, string email, string firstName, CancellationToken cancellationToken)
    {
        var rawToken = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTime.UtcNow.Add(InvitationLifetime);

        await _tokens.CreateAsync(
            userId,
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))),
            expiresAt,
            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            PasswordTokenPurpose.Invite,
            cancellationToken).ConfigureAwait(false);

        var link = $"{_appOptions.WebBaseUrl.TrimEnd('/')}/set-password?token={Uri.EscapeDataString(rawToken)}";
        var days = InvitationLifetime.TotalDays.ToString("0", CultureInfo.InvariantCulture);

        var text =
            $"Hello {firstName},\n\n" +
            $"An account has been created for you on the BCAS Portal.\n\n" +
            $"Set your password to activate it:\n{link}\n\n" +
            $"The link is valid for {days} days and can be used once.\n";

        var html =
            $"<p>Hello {WebUtility.HtmlEncode(firstName)},</p>" +
            $"<p>An account has been created for you on the BCAS Portal.</p>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Set your password</a></p>" +
            $"<p>The link is valid for {days} days and can be used once.</p>";

        try
        {
            await _email.SendAsync(email, "Your BCAS Portal account", html, text, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The account exists either way; the Super Admin can resend from the
            // user list rather than having the whole create fail on a mail error.
            _logger.LogError(ex, "Failed to send the invitation email for user {UserId}.", userId);
        }
    }

    /// <summary>
    /// A real PBKDF2 hash over a random secret that is immediately discarded, so
    /// a provisioned account cannot be signed into until its invitation is used,
    /// and verifying against it costs exactly what a real password costs.
    /// </summary>
    private string UnusablePasswordHash() =>
        _passwordHasher.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Id(int userId) => userId.ToString(CultureInfo.InvariantCulture);
}
