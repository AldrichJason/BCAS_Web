using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using BCAS.Api.Features.ActivityLog;
using BCAS.Api.Features.Email;
using BCAS.Api.Options;
using Microsoft.Extensions.Options;

namespace BCAS.Api.Features.Auth;

public sealed class AuthService : IAuthService
{
    /// <summary>
    /// Verified against when the email is unknown, so an unknown email costs the same
    /// work as a known one and cannot be spotted by response time.
    /// </summary>
    private const string DummyHash =
        "pbkdf2-sha256$210000$AAAAAAAAAAAAAAAAAAAAAA==$3q2+7wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenRepository _resetTokens;
    private readonly ITokenRevocationStore _revokedTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokens;
    private readonly IActivityLogger _activityLog;
    private readonly IEmailSender _email;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LoginOptions _loginOptions;
    private readonly PasswordResetOptions _resetOptions;
    private readonly AppOptions _appOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IPasswordResetTokenRepository resetTokens,
        ITokenRevocationStore revokedTokens,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokens,
        IActivityLogger activityLog,
        IEmailSender email,
        IHttpContextAccessor httpContextAccessor,
        IOptions<LoginOptions> loginOptions,
        IOptions<PasswordResetOptions> resetOptions,
        IOptions<AppOptions> appOptions,
        ILogger<AuthService> logger)
    {
        _users = users;
        _resetTokens = resetTokens;
        _revokedTokens = revokedTokens;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _activityLog = activityLog;
        _email = email;
        _httpContextAccessor = httpContextAccessor;
        _loginOptions = loginOptions.Value;
        _resetOptions = resetOptions.Value;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    // --- BW-10: login --------------------------------------------------------

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        var user = await _users.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            // Spend the same time as a real verification before reporting failure.
            _passwordHasher.Verify(request.Password, DummyHash);
            await LogFailureAsync(null, email, "unknown email", cancellationToken).ConfigureAwait(false);
            return LoginResult.Failed(LoginFailureReason.InvalidCredentials);
        }

        if (user.LockedOutUntil is { } lockedUntil && lockedUntil > DateTime.UtcNow)
        {
            await LogFailureAsync(user.UserId, email, "locked out", cancellationToken).ConfigureAwait(false);
            return LoginResult.Failed(LoginFailureReason.LockedOut);
        }

        var passwordOk = _passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!passwordOk)
        {
            await _users.RecordFailedLoginAsync(
                user.UserId, _loginOptions.MaxFailedAttempts, _loginOptions.LockoutMinutes, cancellationToken)
                .ConfigureAwait(false);
            await LogFailureAsync(user.UserId, email, "wrong password", cancellationToken).ConfigureAwait(false);
            return LoginResult.Failed(LoginFailureReason.InvalidCredentials);
        }

        // A deactivated account is rejected with the same result as bad credentials,
        // so the response cannot be used to probe which accounts exist.
        if (!user.IsActive)
        {
            await LogFailureAsync(user.UserId, email, "account deactivated", cancellationToken).ConfigureAwait(false);
            return LoginResult.Failed(LoginFailureReason.InvalidCredentials);
        }

        var departments = await _users.GetDepartmentScopeAsync(user.UserId, cancellationToken).ConfigureAwait(false);
        var (token, expiresAt) = _tokens.CreateAccessToken(user, departments);

        await _users.RecordSuccessfulLoginAsync(user.UserId, cancellationToken).ConfigureAwait(false);
        await _activityLog.LogAsync(
            "LoginSucceeded", user.UserId, "User", Id(user.UserId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("User {UserId} signed in with role {RoleCode}.", user.UserId, user.RoleCode);

        return LoginResult.Success(new LoginResponse(token, expiresAt, ToProfile(user, departments)));
    }

    // --- BW-11: logout -------------------------------------------------------

    public async Task LogoutAsync(
        string jti, int userId, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        await _revokedTokens.RevokeAsync(jti, userId, expiresAtUtc, cancellationToken).ConfigureAwait(false);
        await _activityLog.LogAsync(
            "LoggedOut", userId, "User", Id(userId), cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("User {UserId} signed out.", userId);
    }

    // --- BW-12: session check ------------------------------------------------

    public async Task<AuthenticatedUser?> GetSessionAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);

        // Deactivated since the token was issued: the token is still signed and
        // unexpired, but it no longer represents a usable account.
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var departments = await _users.GetDepartmentScopeAsync(userId, cancellationToken).ConfigureAwait(false);
        return ToProfile(user, departments);
    }

    // --- BW-13: forgot / reset password --------------------------------------

    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var trimmed = email.Trim();
        var user = await _users.FindByEmailAsync(trimmed, cancellationToken).ConfigureAwait(false);

        // No link for unknown or deactivated accounts - but the caller is told
        // nothing either way, so this cannot be used to enumerate accounts.
        if (user is null || !user.IsActive)
        {
            await _activityLog.LogAsync(
                "PasswordResetRequested", user?.UserId, "User", user is null ? null : Id(user.UserId),
                $"{trimmed}: no link sent", cancellationToken).ConfigureAwait(false);
            return;
        }

        var rawToken = GenerateResetToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_resetOptions.TokenLifetimeMinutes);

        await _resetTokens.CreateAsync(
            user.UserId, HashResetToken(rawToken), expiresAt, CallerIp(), cancellationToken).ConfigureAwait(false);

        var link = $"{_appOptions.WebBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var hours = _resetOptions.TokenLifetimeMinutes / 60d;
        var validFor = hours >= 1
            ? $"{hours:0.#} hour(s)"
            : $"{_resetOptions.TokenLifetimeMinutes} minute(s)";

        var text =
            $"Hello {user.FirstName},\n\n" +
            $"We received a request to reset your BCAS Portal password.\n\n" +
            $"Open this link to choose a new password:\n{link}\n\n" +
            $"The link is valid for {validFor} and can be used once.\n" +
            $"If you did not ask for this, you can ignore this email - your password stays unchanged.\n";

        var html =
            $"<p>Hello {WebUtility.HtmlEncode(user.FirstName)},</p>" +
            $"<p>We received a request to reset your BCAS Portal password.</p>" +
            $"<p><a href=\"{WebUtility.HtmlEncode(link)}\">Choose a new password</a></p>" +
            $"<p>The link is valid for {validFor} and can be used once.</p>" +
            $"<p>If you did not ask for this, you can ignore this email &mdash; your password stays unchanged.</p>";

        try
        {
            await _email.SendAsync(user.Email, "Reset your BCAS Portal password", html, text, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The caller still gets the same neutral response; a failed send must
            // not reveal that the address exists.
            _logger.LogError(ex, "Failed to send a password reset email for user {UserId}.", user.UserId);
        }

        await _activityLog.LogAsync(
            "PasswordResetRequested", user.UserId, "User", Id(user.UserId), "link sent", cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PasswordResetResult> ResetPasswordAsync(
        ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return new PasswordResetResult(PasswordResetOutcome.PasswordMismatch, "The two passwords do not match.");
        }

        if (PasswordPolicy.Validate(request.NewPassword) is { } policyError)
        {
            return new PasswordResetResult(PasswordResetOutcome.WeakPassword, policyError);
        }

        var record = await _resetTokens
            .FindByHashAsync(HashResetToken(request.Token), cancellationToken).ConfigureAwait(false);

        if (record is null || !record.IsUsable(DateTime.UtcNow))
        {
            await _activityLog.LogAsync(
                "PasswordResetFailed", record?.UserId, "User", record is null ? null : Id(record.UserId),
                "invalid or expired token", cancellationToken).ConfigureAwait(false);
            return new PasswordResetResult(PasswordResetOutcome.InvalidOrExpiredToken);
        }

        var user = await _users.FindByIdAsync(record.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || !user.IsActive)
        {
            return new PasswordResetResult(PasswordResetOutcome.AccountInactive);
        }

        // Consuming first is what makes the token single-use: if a concurrent
        // request already spent it, this returns false and no password changes.
        var consumed = await _resetTokens.ConsumeAsync(record.TokenId, cancellationToken).ConfigureAwait(false);
        if (!consumed)
        {
            return new PasswordResetResult(PasswordResetOutcome.InvalidOrExpiredToken);
        }

        await _users.UpdatePasswordAsync(user.UserId, _passwordHasher.Hash(request.NewPassword), cancellationToken)
            .ConfigureAwait(false);

        await _activityLog.LogAsync(
            "PasswordReset", user.UserId, "User", Id(user.UserId), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("User {UserId} reset their password.", user.UserId);

        return new PasswordResetResult(PasswordResetOutcome.Success);
    }

    // --- helpers -------------------------------------------------------------

    private static AuthenticatedUser ToProfile(UserCredentialRecord user, IReadOnlyList<DepartmentScope> departments) =>
        new(
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.RoleCode,
            user.RoleName,
            user.MustChangePassword,
            departments);

    /// <summary>256 bits of entropy, URL-safe so it survives being put in a link.</summary>
    private static string GenerateResetToken() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Only the hash is stored, so the table cannot be used to reset anyone's
    /// password. The token is high-entropy and random, so a plain SHA-256 is
    /// enough here - unlike a user-chosen password, it is not guessable.
    /// </summary>
    private static string HashResetToken(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Id(int userId) => userId.ToString(CultureInfo.InvariantCulture);

    private string? CallerIp() =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private Task LogFailureAsync(int? userId, string email, string reason, CancellationToken cancellationToken) =>
        // The email is recorded because it was submitted, not looked up; the password never is.
        _activityLog.LogAsync(
            "LoginFailed", userId, "User", userId is null ? null : Id(userId.Value),
            $"{email}: {reason}", cancellationToken);
}
