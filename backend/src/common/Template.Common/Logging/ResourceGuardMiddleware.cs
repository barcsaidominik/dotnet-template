using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace Template.Common.Logging;

public sealed class ResourceGuardMiddleware(
    RequestDelegate next,
    ILogger<ResourceGuardMiddleware> logger,
    IOptions<ResourceGuardOptions> options,
    CriticalLoadState criticalLoadState)
{
    private static int _inFlightRequests;

    private readonly RequestDelegate _next = next;
    private readonly ILogger<ResourceGuardMiddleware> _logger = logger;
    private readonly ResourceGuardOptions _options = options.Value;
    private readonly CriticalLoadState _criticalLoadState = criticalLoadState;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var currentInFlight = Interlocked.Increment(ref _inFlightRequests);
        var effectiveRequestToken = _criticalLoadState.CreateLinkedToken(context.RequestAborted, out var linkedCts);
        context.RequestAborted = effectiveRequestToken;

        try
        {
            if (ShouldRejectRequest(currentInFlight, out var rejectionReason, out var workingSetMb, out var availableWorkerThreads))
            {
                _criticalLoadState.EnterCriticalState(
                    rejectionReason,
                    TimeSpan.FromSeconds(_options.CriticalStateDurationSeconds));

                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/problem+json";
                context.Response.Headers.RetryAfter = _options.CriticalStateDurationSeconds.ToString();

                using (LogContext.PushProperty("GuardReason", rejectionReason))
                using (LogContext.PushProperty("CurrentInFlightRequests", currentInFlight))
                using (LogContext.PushProperty("WorkingSetMb", workingSetMb))
                using (LogContext.PushProperty("AvailableWorkerThreads", availableWorkerThreads))
                {
                    _logger.LogWarning(
                        "Request rejected by resource guard. Reason: {GuardReason}, InFlightRequests: {InFlightRequests}, WorkingSetMb: {WorkingSetMb:0.00}, AvailableWorkerThreads: {AvailableWorkerThreads}",
                        rejectionReason,
                        currentInFlight,
                        workingSetMb,
                        availableWorkerThreads);
                }

                var problem = new ProblemDetails
                {
                    Title = "Service temporarily overloaded",
                    Detail = "The server is under high load. Please retry shortly.",
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Type = "https://httpstatuses.com/503"
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
                return;
            }

            await _next(context);
        }
        finally
        {
            linkedCts.Dispose();
            Interlocked.Decrement(ref _inFlightRequests);
        }
    }

    private bool ShouldRejectRequest(int currentInFlight, out string rejectionReason, out double workingSetMb, out int availableWorkerThreads)
    {
        ThreadPool.GetAvailableThreads(out availableWorkerThreads, out _);

        var process = Process.GetCurrentProcess();
        workingSetMb = process.WorkingSet64 / (1024d * 1024d);

        if (_criticalLoadState.IsActive)
        {
            rejectionReason = _criticalLoadState.Reason ?? "CriticalStateActive";
            return true;
        }

        if (workingSetMb >= _options.MaxWorkingSetMb)
        {
            rejectionReason = "WorkingSetLimitExceeded";
            return true;
        }

        if (currentInFlight > _options.MaxConcurrentRequests)
        {
            rejectionReason = "ConcurrentRequestLimitExceeded";
            return true;
        }

        if (availableWorkerThreads <= _options.MinAvailableWorkerThreads)
        {
            rejectionReason = "WorkerThreadStarvationProtection";
            return true;
        }

        rejectionReason = string.Empty;
        return false;
    }

    private bool ShouldSkip(PathString path)
    {
        var requestPath = path.Value ?? string.Empty;

        return _options.ExcludedPaths.Any(excludedPath =>
            requestPath.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
    }
}
