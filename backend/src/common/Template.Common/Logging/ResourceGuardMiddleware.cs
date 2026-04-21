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
    CriticalLoadState criticalLoadState)
{
    private static int _inFlightRequests;

    private static readonly Lock _circuitLock = new();
    private static CircuitState _circuitState = CircuitState.Closed;
    private static DateTime? _openedAt;
    private static TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
    private static DateTime _lastCpuCheckTime = DateTime.UtcNow;

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

        var cpuPercent = GetCpuUsagePercent();
        var memoryPercent = GetMemoryUsagePercent();

        var circuitResult = EvaluateCircuitBreaker(cpuPercent, memoryPercent);
        if (circuitResult.ShouldReject)
        {
            await WriteCircuitBreakerResponse(context, circuitResult.RetryAfterSeconds);
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
            Interlocked.Decrement(ref _inFlightRequests);
        }
    }

    private (bool ShouldReject, int RetryAfterSeconds, bool WasHalfOpen) EvaluateCircuitBreaker(double cpuPercent, double memoryPercent)
    {
        lock (_circuitLock)
        {
            var now = DateTime.UtcNow;

            switch (_circuitState)
            {
                case CircuitState.Closed:
                    if (cpuPercent > _options.CpuThresholdPercent || memoryPercent > _options.MemoryThresholdPercent)
                    {
                        _circuitState = CircuitState.Open;
                        _openedAt = now;
                        _logger.LogWarning("Circuit breaker opened: CPU={Cpu}%, RAM={Ram}%", cpuPercent, memoryPercent);
                        return (true, _options.CircuitBreakerDurationSeconds, false);
                    }
                    return (false, 0, false);

                case CircuitState.Open:
                    if (_openedAt.HasValue && (now - _openedAt.Value).TotalSeconds >= _options.CircuitBreakerDurationSeconds)
                    {
                        _circuitState = CircuitState.HalfOpen;
                        return (false, 0, true);
                    }
                    var remainingSeconds = _openedAt.HasValue
                        ? Math.Max(1, (int)Math.Ceiling(_options.CircuitBreakerDurationSeconds - (now - _openedAt.Value).TotalSeconds))
                        : _options.CircuitBreakerDurationSeconds;
                    return (true, remainingSeconds, false);

                case CircuitState.HalfOpen:
                    return (true, _options.CircuitBreakerDurationSeconds, false);

                default:
                    return (false, 0, false);
            }
        }
    }

    private void TransitionFromHalfOpen(double cpuPercent, double memoryPercent)
    {
        lock (_circuitLock)
        {
            if (_circuitState != CircuitState.HalfOpen)
            {
                return;
            }

            if (cpuPercent <= _options.CpuThresholdPercent && memoryPercent <= _options.MemoryThresholdPercent)
            {
                _circuitState = CircuitState.Closed;
                _openedAt = null;
                _logger.LogInformation("Circuit breaker closed");
            }
            else
            {
                _circuitState = CircuitState.Open;
                _openedAt = DateTime.UtcNow;
                _logger.LogWarning("Circuit breaker reopened: CPU={Cpu}%, RAM={Ram}%", cpuPercent, memoryPercent);
            }
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

    private static double GetCpuUsagePercent()
    {
        var process = Process.GetCurrentProcess();
        var currentTotalProcessorTime = process.TotalProcessorTime;
        var currentTime = DateTime.UtcNow;

        TimeSpan lastProcessorTime;
        DateTime lastCheckTime;

        lock (_circuitLock)
        {
            lastProcessorTime = _lastTotalProcessorTime;
            lastCheckTime = _lastCpuCheckTime;
            _lastTotalProcessorTime = currentTotalProcessorTime;
            _lastCpuCheckTime = currentTime;
        }

        var cpuUsedMs = (currentTotalProcessorTime - lastProcessorTime).TotalMilliseconds;
        var elapsedMs = (currentTime - lastCheckTime).TotalMilliseconds;

        if (elapsedMs <= 0)
        {
            return 0;
        }

        var processorCount = Environment.ProcessorCount;
        var cpuPercent = (cpuUsedMs / (elapsedMs * processorCount)) * 100;

        return Math.Min(100, Math.Max(0, cpuPercent));
    }

    private static double GetMemoryUsagePercent()
    {
        var process = Process.GetCurrentProcess();
        var workingSet = process.WorkingSet64;

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
