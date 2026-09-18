using BCAS.Api.Features.ActivityLog;
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
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokens;
    private readonly IActivityLogger _activityLog;
    private readonly LoginOptions _loginOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokens,
        IActivityLogger activityLog,
        IOptions<LoginOptions> loginOptions,
        ILogger<AuthService> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _activityLog = activityLog;
        _loginOptions = loginOptions.Value;
        _logger = logger;
    }

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
            "LoginSucceeded", user.UserId, "User", user.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("User {UserId} signed in with role {RoleCode}.", user.UserId, user.RoleCode);

        var profile = new AuthenticatedUser(
            user.UserId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.RoleCode,
            user.RoleName,
            user.MustChangePassword,
            departments);

        return LoginResult.Success(new LoginResponse(token, expiresAt, profile));
    }

    private Task LogFailureAsync(int? userId, string email, string reason, CancellationToken cancellationToken) =>
        // The email is recorded because it was submitted, not looked up; the password never is.
        _activityLog.LogAsync(
            "LoginFailed", userId, "User", userId?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            $"{email}: {reason}", cancellationToken);
}
