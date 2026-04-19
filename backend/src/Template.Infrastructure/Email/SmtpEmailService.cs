using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Template.Application.Common.Interfaces;

namespace Template.Infrastructure.Email;

public sealed class SmtpEmailService(IOptions<EmailSettings> settings, ISetupInvitationEmailTemplateFactory templateFactory) : IEmailService
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task SendSetupEmailAsync(string toEmail, string setupLink, CancellationToken ct = default)
    {
        var template = await templateFactory.CreateAsync(setupLink, ct);

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.From),
            Subject = template.Subject,
            Body = template.HtmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = true
        };

        await client.SendMailAsync(message, ct);
    }
}
