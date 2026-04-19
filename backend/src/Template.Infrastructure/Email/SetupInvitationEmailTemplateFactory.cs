using System.Globalization;
using System.Net;
using Template.Infrastructure.Templating;

namespace Template.Infrastructure.Email;

public sealed class SetupInvitationEmailTemplateFactory(ITemplateRenderer templateRenderer) : ISetupInvitationEmailTemplateFactory
{
    private const string TEMPLATE_GROUP = "Email";
    private const string TEMPLATE_NAME = "setup-invitation";
    private const string SUBJECT_PART = "subject";
    private const string BODY_PART = "html";

    public async Task<EmailTemplateContent> CreateAsync(string setupLink, CancellationToken ct = default)
    {
        var culture = CultureInfo.CurrentUICulture;
        var model = new SetupInvitationTemplateModel(WebUtility.HtmlEncode(setupLink));

        var subjectTask = templateRenderer.RenderAsync(TEMPLATE_GROUP, TEMPLATE_NAME, SUBJECT_PART, model, culture, ct);
        var htmlTask = templateRenderer.RenderAsync(TEMPLATE_GROUP, TEMPLATE_NAME, BODY_PART, model, culture, ct);

        await Task.WhenAll(subjectTask, htmlTask);

        var subject = await subjectTask;
        var htmlBody = await htmlTask;

        return new EmailTemplateContent(subject.Trim(), htmlBody);
    }
}
