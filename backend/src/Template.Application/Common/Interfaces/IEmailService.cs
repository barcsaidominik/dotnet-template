namespace Template.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendSetupEmailAsync(string toEmail, string setupLink, CancellationToken ct = default);
}
