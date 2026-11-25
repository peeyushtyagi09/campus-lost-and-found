using LostAndFound.Api.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LostAndFound.Api.Services;

public interface IEmailSender
{
    Task SendGenericNoticeAsync(
        string recipientEmail,
        string recipientUserId,
        string subject,
        CancellationToken cancellationToken);
}

public sealed class MailKitOrConsoleEmailSender : IEmailSender
{
    private const string GenericBody =
        "You have a new update on the Campus Lost & Found Platform. Please log in to view details.";

    private readonly EmailOptions _options;
    private readonly ILogger<MailKitOrConsoleEmailSender> _logger;

    public MailKitOrConsoleEmailSender(
        IOptions<EmailOptions> options,
        ILogger<MailKitOrConsoleEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendGenericNoticeAsync(
        string recipientEmail,
        string recipientUserId,
        string subject,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Console email notice for user {UserId}: {Subject} — log in to view.",
                recipientUserId,
                subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.FromAddress));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = GenericBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);
        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
