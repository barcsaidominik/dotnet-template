namespace Template.Application.Common.Notifications;

public interface INotificationService
{
    Task SendAsync(NotificationRequest request, CancellationToken ct = default);
}
