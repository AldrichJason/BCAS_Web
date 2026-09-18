namespace BCAS.Api.Helpers;

/// <summary>
/// Development fallback used when <c>Email:Enabled</c> is false. Writes the
/// message to the application log so a reset link can be picked up from the
/// console without an SMTP server.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(
        string toAddress, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email sending is disabled. Would have sent to {Recipient} with subject \"{Subject}\":\n{Body}",
            toAddress, subject, textBody);
        return Task.CompletedTask;
    }
}
