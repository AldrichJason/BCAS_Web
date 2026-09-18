namespace BCAS.Api.Options;

/// <summary>Bound from the "Email" configuration section.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// When false, outgoing mail is written to the application log instead of
    /// being sent. This is the default for local development.
    /// </summary>
    public bool Enabled { get; set; }

    public string FromAddress { get; set; } = "no-reply@bcas.edu.ph";

    public string FromName { get; set; } = "BCAS Portal";

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool UseStartTls { get; set; } = true;

    public string UserName { get; set; } = string.Empty;

    /// <summary>Supplied through user secrets or the deployment secret store.</summary>
    public string Password { get; set; } = string.Empty;
}
