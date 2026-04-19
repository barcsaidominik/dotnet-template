using Template.Application.Common.Dtos;

namespace Template.Application.Common.Interfaces;

public interface IBackgroundJobScheduler
{
    Task<QueuedBackgroundJobResult> ScheduleDemoLongRunningOperationAsync(CancellationToken ct = default);
}
