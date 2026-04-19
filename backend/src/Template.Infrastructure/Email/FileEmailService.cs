using Template.Application.Common.Interfaces;

namespace Template.Infrastructure.Email;

public sealed class FileEmailService(ISetupInvitationEmailTemplateFactory templateFactory) : IEmailService
{
    public async Task SendSetupEmailAsync(string toEmail, string setupLink, CancellationToken ct = default)
    {
        var template = await templateFactory.CreateAsync(setupLink, ct);

        var emailsDir = Path.Combine(Path.GetTempPath(), "emails");
        Directory.CreateDirectory(emailsDir);

        var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{toEmail}.html";
        var filePath = Path.Combine(emailsDir, fileName);

        await File.WriteAllTextAsync(filePath, template.HtmlBody, ct);
    }
}
