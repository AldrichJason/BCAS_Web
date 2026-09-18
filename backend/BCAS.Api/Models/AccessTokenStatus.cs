namespace BCAS.Api.Models;

public enum AccessTokenStatus
{
    /// <summary>The token may be used.</summary>
    Accepted,

    /// <summary>The token was logged out (BW-11).</summary>
    Revoked,

    /// <summary>The account behind the token is deactivated or gone (BW-15).</summary>
    AccountInactive,
}
