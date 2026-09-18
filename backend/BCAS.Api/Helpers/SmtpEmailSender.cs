using System.Net;
using System.Net.Mail;
using BCAS.Api.Options;
using Microsoft.Extensions.Options;

namespace BCAS.Api.Helpers;

/// <summary>Sends mail over SMTP. Used when <c>Email:Enabled</c> is true.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.SmtpHost))
        {
            throw new InvalidOperationException("Email:SmtpHost must be set when Email:Enabled is true.");
        }
    }

    public async Task SendAsync(
        string toAddress, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseStartTls,
            Credentials = string.IsNullOrWhiteSpace(_options.UserName)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.UserName, _options.Password),
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toAddress);
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain"));

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Sent \"{Subject}\" to {Recipient}.", subject, Mask(toAddress));
    }

    private static string Mask(string email)
    {
        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at <= 1 ? "***" : $"{email[0]}***{email[at..]}";
    }
}
