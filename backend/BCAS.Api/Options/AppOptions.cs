namespace BCAS.Api.Options;

/// <summary>Bound from the "App" configuration section.</summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// Public base URL of the React client, used to build links that are emailed
    /// to users (for example the password reset link).
    /// </summary>
    public string WebBaseUrl { get; set; } = "http://localhost:5173";
}
