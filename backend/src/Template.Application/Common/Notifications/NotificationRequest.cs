using System.Globalization;

namespace Template.Application.Common.Notifications;

public sealed record NotificationRequest(
    NotificationTemplateKey TemplateKey,
    NotificationRecipient Recipient,
    object Model,
    CultureInfo? Culture,
    NotificationChannelType[] Channels);
