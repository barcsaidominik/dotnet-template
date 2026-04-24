using Template.Application.Common.Notifications;
using Template.Common.Email;
using Template.Infrastructure.Email;

namespace Template.Infrastructure.Notifications.Channels;

public sealed class EmailNotificationChannel(
    IEmailService emailService,
    ISetupInvitationEmailTemplateFactory setupInvitationTemplateFactory,
    IPasswordResetEmailTemplateFactory passwordResetTemplateFactory) : INotificationChannel
{
    private readonly IEmailService _emailService = emailService;
    private readonly ISetupInvitationEmailTemplateFactory _setupInvitationTemplateFactory = setupInvitationTemplateFactory;
    private readonly IPasswordResetEmailTemplateFactory _passwordResetTemplateFactory = passwordResetTemplateFactory;

    public bool CanHandle(NotificationChannelType channelType)
    {
        return channelType == NotificationChannelType.Email;
    }

    public async Task SendAsync(NotificationRequest request, CancellationToken ct = default)
    {
        if (request.TemplateKey == NotificationTemplateKey.SetupInvitation)
        {
            if (request.Model is not SetupInvitationNotificationModel model)
            {
                throw new InvalidOperationException(
                    "A SetupInvitation értesítéshez SetupInvitationNotificationModel szükséges.");
            }

            var template = await _setupInvitationTemplateFactory.CreateAsync(model.SetupLink, ct);
            await _emailService.SendAsync(request.Recipient.Email, template.Subject, template.HtmlBody, ct);
            return;
        }

        if (request.TemplateKey == NotificationTemplateKey.PasswordReset)
        {
            if (request.Model is not PasswordResetNotificationModel model)
            {
                throw new InvalidOperationException(
                    "A PasswordReset értesítéshez PasswordResetNotificationModel szükséges.");
            }

            var template = await _passwordResetTemplateFactory.CreateAsync(model.ResetLink, ct);
            await _emailService.SendAsync(request.Recipient.Email, template.Subject, template.HtmlBody, ct);
            return;
        }

        throw new NotSupportedException(
            $"A(z) '{request.TemplateKey}' sablon még nem támogatott email csatornán.");
    }
}
