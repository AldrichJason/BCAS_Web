namespace BCAS.Api.Features.Auth;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the caller's access token so it cannot be replayed before it
    /// expires (BW-11). Safe to call twice with the same token.
    /// </summary>
    Task LogoutAsync(string jti, int userId, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-reads the signed-in user for the session check (BW-12). Returns null
    /// when the account has been deactivated since the token was issued.
    /// </summary>
    Task<AuthenticatedUser?> GetSessionAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a reset link when the account exists (BW-13). Completes the same
    /// way whether or not it does, so callers cannot enumerate accounts.
    /// </summary>
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);

    Task<PasswordResetResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
