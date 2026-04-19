using Template.Application.Common.Interfaces;

namespace Template.Infrastructure.Email;

public sealed class FileEmailService : IEmailService
{
    public async Task SendSetupEmailAsync(string toEmail, string setupLink, CancellationToken ct = default)
    {
        var html = BuildHtmlEmail(setupLink);

        var emailsDir = Path.Combine(Path.GetTempPath(), "emails");
        Directory.CreateDirectory(emailsDir);

        var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{toEmail}.html";
        var filePath = Path.Combine(emailsDir, fileName);

        await File.WriteAllTextAsync(filePath, html, ct);
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
