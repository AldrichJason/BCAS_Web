using BCAS.Api.Models.DTOs;

namespace BCAS.Api.Models;

public enum LoginFailureReason
{
    /// <summary>Unknown email, wrong password, or a deactivated account.</summary>
    InvalidCredentials,

    /// <summary>Too many consecutive failures; the account is temporarily locked.</summary>
    LockedOut,
}

/// <summary>
/// Outcome of a login attempt. Callers must not expose <see cref="LoginFailureReason"/>
/// beyond the two messages defined in the controller - the distinction between an
/// unknown email, a wrong password and a deactivated account never reaches the client.
/// </summary>
public sealed class LoginResult
{
    private LoginResult(LoginResponse? response, LoginFailureReason? failure)
    {
        Response = response;
        Failure = failure;
    }

    public LoginResponse? Response { get; }

    public LoginFailureReason? Failure { get; }

    public bool Succeeded => Response is not null;

    public static LoginResult Success(LoginResponse response) => new(response, null);

    public static LoginResult Failed(LoginFailureReason reason) => new(null, reason);
}
