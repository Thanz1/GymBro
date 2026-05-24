using System.Net;
using System.Net.Mail;
using GymBro.Contracts.Events;
using Microsoft.Extensions.Options;

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

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = "Chao mung den voi GymBro",
            Body = BuildBody(integrationEvent),
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(integrationEvent.Email, integrationEvent.FullName));

        using var smtpClient = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.UserName, _options.Password)
        };

        await smtpClient.SendMailAsync(message, cancellationToken);
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
