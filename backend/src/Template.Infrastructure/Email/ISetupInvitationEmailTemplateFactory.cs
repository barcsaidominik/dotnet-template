namespace Template.Infrastructure.Email;

public interface ISetupInvitationEmailTemplateFactory
{
    Task<EmailTemplateContent> CreateAsync(string setupLink, CancellationToken ct = default);
}
