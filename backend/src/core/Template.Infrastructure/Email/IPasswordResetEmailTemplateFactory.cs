using Template.Common.Email;

namespace Template.Infrastructure.Email;

public interface IPasswordResetEmailTemplateFactory
{
    Task<EmailTemplateContent> CreateAsync(string resetLink, CancellationToken ct = default);
}
