namespace BCAS.Api.Features.Auth;

/// <summary>
/// Tracks access tokens that have been logged out. Access tokens are stateless,
/// so this is what makes "sign out" take effect before the token expires.
/// </summary>
public interface ITokenRevocationStore
{
    Task RevokeAsync(string jti, int userId, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default);
}
