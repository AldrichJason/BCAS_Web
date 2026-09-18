namespace BCAS.Api.Options;

/// <summary>Bound from the "PasswordReset" configuration section.</summary>
public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";

    /// <summary>How long a reset link stays valid.</summary>
    public int TokenLifetimeMinutes { get; set; } = 60;
}
