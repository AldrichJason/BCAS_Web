namespace BCAS.Api.Options;

/// <summary>Bound from the "Login" configuration section.</summary>
public sealed class LoginOptions
{
    public const string SectionName = "Login";

    /// <summary>Consecutive failures before the account is locked out.</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;
}
