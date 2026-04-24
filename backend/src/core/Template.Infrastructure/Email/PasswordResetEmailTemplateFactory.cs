using System.Globalization;
using System.Net;
using Template.Common.Email;
using Template.Common.Templating;

namespace Template.Infrastructure.Email;

public sealed class PasswordResetEmailTemplateFactory(ITemplateRenderer templateRenderer) : IPasswordResetEmailTemplateFactory
{
    private const string TEMPLATE_GROUP = "Email";
    private const string TEMPLATE_NAME = "password-reset";
    private const string SUBJECT_PART = "subject";
    private const string BODY_PART = "html";

    public async Task<EmailTemplateContent> CreateAsync(string resetLink, CancellationToken ct = default)
    {
        var culture = CultureInfo.CurrentUICulture;
        var model = new PasswordResetTemplateModel(WebUtility.HtmlEncode(resetLink));

        var subjectTask = templateRenderer.RenderAsync(TEMPLATE_GROUP, TEMPLATE_NAME, SUBJECT_PART, model, culture, ct);
        var htmlTask = templateRenderer.RenderAsync(TEMPLATE_GROUP, TEMPLATE_NAME, BODY_PART, model, culture, ct);

        await Task.WhenAll(subjectTask, htmlTask);

        var subject = await subjectTask;
        var htmlBody = await htmlTask;

        return new EmailTemplateContent(subject.Trim(), htmlBody);
    }
}
