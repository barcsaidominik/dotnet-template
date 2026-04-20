using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Template.Common.Email;

public sealed class SmtpEmailService(IOptions<EmailSettings> settings) : IEmailService
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_settings.From),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = true
        };

        await client.SendMailAsync(message, ct);
    }
}
