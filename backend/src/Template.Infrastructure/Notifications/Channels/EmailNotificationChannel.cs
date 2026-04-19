using Template.Application.Common.Interfaces;
using Template.Application.Common.Notifications;

namespace Template.Infrastructure.Notifications.Channels;

public sealed class EmailNotificationChannel(IEmailService emailService) : INotificationChannel
{
    private readonly IEmailService _emailService = emailService;

    public bool CanHandle(NotificationChannelType channelType)
    {
        return channelType == NotificationChannelType.Email;
    }

    public Task SendAsync(NotificationRequest request, CancellationToken ct = default)
    {
        return request.TemplateKey switch
        {
            NotificationTemplateKey.SetupInvitation when request.Model is SetupInvitationNotificationModel model
                => _emailService.SendSetupEmailAsync(request.Recipient.Email, model.SetupLink, ct),
            NotificationTemplateKey.SetupInvitation
                => throw new InvalidOperationException("A SetupInvitation értesítéshez SetupInvitationNotificationModel szükséges."),
            _ => throw new NotSupportedException($"A(z) '{request.TemplateKey}' sablon még nem támogatott email csatornán.")
        };
    }
}
