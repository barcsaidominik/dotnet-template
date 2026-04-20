using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using TickerQ.Utilities;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace Template.Common.Jobs;

public sealed class TickerQBackgroundJobScheduler(
    ITimeTickerManager<TimeTickerEntity> timeTickerManager,
    ICurrentUserService currentUserService)
    : IBackgroundJobScheduler
{
    private const string DEMO_LONG_RUNNING_FUNCTION = "Demo.LongRunningOperation";

    private readonly ITimeTickerManager<TimeTickerEntity> _timeTickerManager = timeTickerManager;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<QueuedBackgroundJobResult> ScheduleDemoLongRunningOperationAsync(CancellationToken ct = default)
    {
        var scheduledAtUtc = DateTime.UtcNow.AddSeconds(5);
        var request = new DemoJobRequest(_currentUserService.UserId, DateTime.UtcNow);

        var result = await _timeTickerManager.AddAsync(new TimeTickerEntity
        {
            Function = DEMO_LONG_RUNNING_FUNCTION,
            Description = "Demo long-running background operation scheduled from the admin API.",
            ExecutionTime = scheduledAtUtc,
            Request = TickerHelper.CreateTickerRequest(request),
            Retries = 1,
            RetryIntervals = [30]
        }, ct);

        if (!result.IsSucceeded || result.Result is null)
        {
            throw result.Exception ?? new InvalidOperationException("TickerQ failed to schedule the background job.");
        }

        return new QueuedBackgroundJobResult(result.Result.Id, DEMO_LONG_RUNNING_FUNCTION, scheduledAtUtc);
    }
}
