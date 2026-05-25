using System.Net;
using GymBro.Contracts.Events;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GymBro.Identity.API.Email;

public sealed class SmtpWelcomeEmailSender : IWelcomeEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpWelcomeEmailSender> _logger;

    public SmtpWelcomeEmailSender(
        IOptions<SmtpOptions> options,
        ILogger<SmtpWelcomeEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(
        UserCreatedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Welcome email is disabled. Skipped Email={Email}",
                integrationEvent.Email);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.UserName) ||
            string.IsNullOrWhiteSpace(_options.Password) ||
            string.IsNullOrWhiteSpace(_options.FromEmail))
        {
            _logger.LogWarning(
                "SMTP settings are incomplete. Skipped welcome email for Email={Email}",
                integrationEvent.Email);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        message.To.Add(new MailboxAddress(integrationEvent.FullName, integrationEvent.Email));
        message.Subject = "Chao mung den voi GymBro";
        message.Body = new BodyBuilder
        {
            HtmlBody = BuildBody(integrationEvent)
        }.ToMessageBody();

        using var smtpClient = new SmtpClient();
        var secureSocketOptions = _options.EnableSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        await smtpClient.ConnectAsync(
            _options.Host,
            _options.Port,
            secureSocketOptions,
            cancellationToken);
        await smtpClient.AuthenticateAsync(
            new NetworkCredential(_options.UserName, _options.Password),
            cancellationToken);
        await smtpClient.SendAsync(message, cancellationToken);
        await smtpClient.DisconnectAsync(quit: true, cancellationToken);

        _logger.LogInformation(
            "Sent welcome email to Email={Email}, UserId={UserId}",
            integrationEvent.Email,
            integrationEvent.UserId);
    }

    private static string BuildBody(UserCreatedIntegrationEvent integrationEvent)
    {
        var displayName = string.IsNullOrWhiteSpace(integrationEvent.FullName)
            ? integrationEvent.Username
            : integrationEvent.FullName;

        return $"""
            <div style="font-family:Arial,sans-serif;line-height:1.6;color:#222">
                <h2>Chao mung den voi GymBro, {WebUtility.HtmlEncode(displayName)}!</h2>
                <p>Tai khoan cua ban da duoc tao thanh cong.</p>
                <p>Cam on ban da tham gia GymBro. Chuc ban mua sam va tap luyen that sung.</p>
                <p style="margin-top:24px">GymBro Team</p>
            </div>
            """;
    }
}
