using Template.Application.Common.Notifications;

namespace Template.Infrastructure.Notifications.Channels;

public interface INotificationChannel
{
    bool CanHandle(NotificationChannelType channelType);

    Task SendAsync(NotificationRequest request, CancellationToken ct = default);
}
