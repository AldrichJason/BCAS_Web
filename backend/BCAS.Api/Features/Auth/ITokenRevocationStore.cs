namespace BCAS.Api.Features.Auth;

public enum AccessTokenStatus
{
    /// <summary>The token may be used.</summary>
    Accepted,

    /// <summary>The token was logged out (BW-11).</summary>
    Revoked,

    /// <summary>The account behind the token is deactivated or gone (BW-15).</summary>
    AccountInactive,
}

/// <summary>
/// Decides whether a signed, unexpired access token may still be used. Access
/// tokens are stateless, so this is what lets a logout (BW-11) or a
/// deactivation (BW-15) take effect before the token expires on its own.
/// </summary>
public interface ITokenRevocationStore
{
    Task RevokeAsync(string jti, int userId, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks revocation and account status together, in one round trip, because
    /// this runs on every authenticated request.
    /// </summary>
    Task<AccessTokenStatus> GetStatusAsync(string jti, int userId, CancellationToken cancellationToken = default);
}
