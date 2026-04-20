using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Template.Application.Common.Interfaces;
using Template.Common.Jobs;
using Template.Infrastructure.Persistence;
using TickerQ.Utilities.Base;

namespace Template.Infrastructure.BackgroundJobs;

public sealed class TickerQDemoJobs(
    ILogger<TickerQDemoJobs> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    private readonly ILogger<TickerQDemoJobs> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    [TickerFunction("Demo.LongRunningOperation", maxConcurrency: 1)]
    public async Task RunLongRunningOperationAsync(
        TickerFunctionContext<DemoJobRequest> context,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mailboxService = scope.ServiceProvider.GetRequiredService<IMailboxService>();

        try
        {
            _logger.LogInformation(
                "TickerQ demo job started. JobId: {JobId}, RequestedByUserId: {RequestedByUserId}, RequestedAtUtc: {RequestedAtUtc}",
                context.Id,
                context.Request.RequestedByUserId,
                context.Request.RequestedAtUtc);

            var facilityCount = await dbContext.Facilities.CountAsync(cancellationToken);
            var productCount = await dbContext.Products.CountAsync(cancellationToken);
            var userCount = await dbContext.Users.CountAsync(cancellationToken);

            for (var step = 1; step <= 5; step++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                _logger.LogInformation(
                    "TickerQ demo job progress. JobId: {JobId}, Step: {Step}/5, Facilities: {FacilityCount}, Products: {ProductCount}, Users: {UserCount}",
                    context.Id,
                    step,
                    facilityCount,
                    productCount,
                    userCount);
            }

            _logger.LogInformation(
                "TickerQ demo job completed. JobId: {JobId}, Facilities: {FacilityCount}, Products: {ProductCount}, Users: {UserCount}",
                context.Id,
                facilityCount,
                productCount,
                userCount);

            if (context.Request.RequestedByUserId is Guid requestedByUserId)
            {
                await mailboxService.AddAsync(
                    requestedByUserId,
                    "BackgroundJobCompleted",
                    "mailbox.events.longRunningJobCompleted.title",
                    "mailbox.events.longRunningJobCompleted.body",
                    new Dictionary<string, string>
                    {
                        ["jobId"] = context.Id.ToString(),
                        ["facilityCount"] = facilityCount.ToString(),
                        ["productCount"] = productCount.ToString(),
                        ["userCount"] = userCount.ToString()
                    },
                    "/mailbox",
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (context.Request.RequestedByUserId is Guid requestedByUserId)
            {
                await mailboxService.AddAsync(
                    requestedByUserId,
                    "BackgroundJobCancelled",
                    "mailbox.events.longRunningJobCancelled.title",
                    "mailbox.events.longRunningJobCancelled.body",
                    new Dictionary<string, string>
                    {
                        ["jobId"] = context.Id.ToString()
                    },
                    "/mailbox");
            }

            throw;
        }
        catch (Exception)
        {
            if (context.Request.RequestedByUserId is Guid requestedByUserId)
            {
                await mailboxService.AddAsync(
                    requestedByUserId,
                    "BackgroundJobFailed",
                    "mailbox.events.longRunningJobFailed.title",
                    "mailbox.events.longRunningJobFailed.body",
                    new Dictionary<string, string>
                    {
                        ["jobId"] = context.Id.ToString()
                    },
                    "/mailbox");
            }

            throw;
        }
    }

    [TickerFunction("Demo.Heartbeat", cronExpression: "0 */5 * * * *", maxConcurrency: 1)]
    public async Task RunHeartbeatAsync(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var facilityCount = await dbContext.Facilities.CountAsync(cancellationToken);
        var productCount = await dbContext.Products.CountAsync(cancellationToken);
        var userCount = await dbContext.Users.CountAsync(cancellationToken);

        _logger.LogInformation(
            "TickerQ heartbeat executed. JobId: {JobId}, Facilities: {FacilityCount}, Products: {ProductCount}, Users: {UserCount}",
            context.Id,
            facilityCount,
            productCount,
            userCount);
    }
}
