using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace Template.Common.Logging;

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen
}

public sealed class ResourceGuardMiddleware(
    RequestDelegate next,
    ILogger<ResourceGuardMiddleware> logger,
    IOptions<ResourceGuardOptions> options,
    CriticalLoadState criticalLoadState,
    CircuitBreakerState circuitBreakerState)
{
    private static readonly Process _currentProcess = Process.GetCurrentProcess();

    private readonly RequestDelegate _next = next;
    private readonly ILogger<ResourceGuardMiddleware> _logger = logger;
    private readonly ResourceGuardOptions _options = options.Value;
    private readonly CriticalLoadState _criticalLoadState = criticalLoadState;
    private readonly CircuitBreakerState _circuitBreakerState = circuitBreakerState;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled || ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var cpuPercent = GetCpuUsagePercent();
        var memoryPercent = GetMemoryUsagePercent();

        var circuitResult = EvaluateCircuitBreaker(cpuPercent, memoryPercent);
        if (circuitResult.ShouldReject)
        {
            await WriteCircuitBreakerResponse(context, circuitResult.RetryAfterSeconds);
            return;
        }

        var currentInFlight = _circuitBreakerState.IncrementInFlight();
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

            if (circuitResult.WasHalfOpen)
            {
                var newCpuPercent = GetCpuUsagePercent();
                var newMemoryPercent = GetMemoryUsagePercent();
                TransitionFromHalfOpen(newCpuPercent, newMemoryPercent);
            }
        }
        finally
        {
            linkedCts.Dispose();
            _circuitBreakerState.DecrementInFlight();
        }
    }

    private (bool ShouldReject, int RetryAfterSeconds, bool WasHalfOpen) EvaluateCircuitBreaker(double cpuPercent, double memoryPercent)
    {
        var (shouldReject, retryAfterSeconds, wasHalfOpen, justOpened) = _circuitBreakerState.EvaluateCircuit(
            cpuPercent, memoryPercent,
            _options.CpuThresholdPercent, _options.MemoryThresholdPercent,
            _options.CircuitBreakerDurationSeconds);

        if (justOpened)
        {
            _logger.LogWarning("Circuit breaker opened: CPU={Cpu}%, RAM={Ram}%", cpuPercent, memoryPercent);
        }

        return (shouldReject, retryAfterSeconds, wasHalfOpen);
    }

    private void TransitionFromHalfOpen(double cpuPercent, double memoryPercent)
    {
        var result = _circuitBreakerState.TryTransitionFromHalfOpen(
            cpuPercent, memoryPercent,
            _options.CpuThresholdPercent, _options.MemoryThresholdPercent);

        if (result is true)
        {
            _logger.LogInformation("Circuit breaker closed");
        }
        else if (result is false)
        {
            _logger.LogWarning("Circuit breaker reopened: CPU={Cpu}%, RAM={Ram}%", cpuPercent, memoryPercent);
        }
    }

    private async Task WriteCircuitBreakerResponse(HttpContext context, int retryAfterSeconds)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";
        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

        var response = new
        {
            type = "ServiceUnavailable",
            title = "Service temporarily unavailable due to high resource usage",
            retryAfter = retryAfterSeconds
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private double GetCpuUsagePercent()
    {
        var currentTotalProcessorTime = _currentProcess.TotalProcessorTime;
        var now = DateTime.UtcNow;

        var (lastProcessorTime, lastCheckTime) = _circuitBreakerState.SnapshotCpuMeasurement(currentTotalProcessorTime, now);

        var cpuUsedMs = (currentTotalProcessorTime - lastProcessorTime).TotalMilliseconds;
        var elapsedMs = (now - lastCheckTime).TotalMilliseconds;

        if (elapsedMs <= 0)
        {
            return 0;
        }

        var cpuPercent = (cpuUsedMs / (elapsedMs * Environment.ProcessorCount)) * 100;
        return Math.Min(100, Math.Max(0, cpuPercent));
    }

    private double GetMemoryUsagePercent()
    {
        var workingSet = _currentProcess.WorkingSet64;

        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            var totalAvailable = gcInfo.TotalAvailableMemoryBytes;

            if (totalAvailable > 0)
            {
                return (workingSet / (double)totalAvailable) * 100;
            }
        }
        catch
        {
            // Fallback if GC info unavailable
        }

        return 0;
    }

    private bool ShouldRejectRequest(int currentInFlight, out string rejectionReason, out double workingSetMb, out int availableWorkerThreads)
    {
        ThreadPool.GetAvailableThreads(out availableWorkerThreads, out _);

        workingSetMb = _currentProcess.WorkingSet64 / (1024d * 1024d);

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
