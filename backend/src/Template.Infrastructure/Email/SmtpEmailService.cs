using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Template.Application.Common.Interfaces;

namespace Template.Infrastructure.Email;

public sealed class SmtpEmailService(IOptions<EmailSettings> settings) : IEmailService
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task SendSetupEmailAsync(string toEmail, string setupLink, CancellationToken ct = default)
    {
        var html = BuildHtmlEmail(setupLink);

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.From),
            Subject = "Meghivo - Template rendszer",
            Body = html,
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

    private static string BuildHtmlEmail(string setupLink)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <title>Meghivo - Template rendszer</title>
            </head>
            <body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;">
                <h1>Udvozoljuk!</h1>
                <p>Meghivtak a Template rendszerbe.</p>
                <p>Az alabbi linkre kattintva veglegesitheti a regisztraciot:</p>
                <p style="margin: 30px 0;">
                    <a href="{setupLink}"
                       style="background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block;">
                        Regisztracio veglegesitese
                    </a>
                </p>
                <p style="color: #666; font-size: 14px;">
                    Ha nem On igenyelte ezt a meghivot, hagyja figyelmen kivul ezt az uzenetet.
                </p>
            </body>
            </html>
            """;
    }
}
