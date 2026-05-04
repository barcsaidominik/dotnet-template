using Microsoft.Extensions.Logging;

namespace Template.Common.Email;

public sealed class FileEmailService(ILogger<FileEmailService> logger) : IEmailService
{
    private readonly ILogger<FileEmailService> _logger = logger;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var emailsDir = Path.Combine(Path.GetTempPath(), "emails");
        Directory.CreateDirectory(emailsDir);

        var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.html";
        var filePath = Path.Combine(emailsDir, fileName);

        await File.WriteAllTextAsync(filePath, $"<h2>{subject}</h2>{htmlBody}", ct);
        _logger.LogInformation("Email saved to {FilePath}", filePath);
    }
}
