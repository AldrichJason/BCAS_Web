namespace BCAS.Api.Features.Auth;

public enum PasswordResetOutcome
{
    Success,

    /// <summary>The token is unknown, already used, superseded or expired.</summary>
    InvalidOrExpiredToken,

    /// <summary>The two password fields did not match.</summary>
    PasswordMismatch,

    /// <summary>The new password does not satisfy <see cref="PasswordPolicy"/>.</summary>
    WeakPassword,

    /// <summary>The account behind the token is deactivated.</summary>
    AccountInactive,
}

/// <summary>Result of a reset attempt; <see cref="Message"/> is safe to display.</summary>
public sealed record PasswordResetResult(PasswordResetOutcome Outcome, string? Message = null)
{
    public bool Succeeded => Outcome == PasswordResetOutcome.Success;
}
