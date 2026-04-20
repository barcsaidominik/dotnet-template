using Template.Application.Common.Dtos;
using Template.Application.Common.Notifications;

namespace Template.Application.Common.Interfaces;

public interface IBackgroundJobScheduler
{
    Task<QueuedBackgroundJobResult> ScheduleDemoLongRunningOperationAsync(CancellationToken ct = default);
    Task<QueuedBackgroundJobResult> ScheduleNotificationAsync(NotificationRequest request, CancellationToken ct = default);
}
