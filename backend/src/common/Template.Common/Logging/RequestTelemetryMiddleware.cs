using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace Template.Common.Logging;

public sealed class RequestTelemetryMiddleware(RequestDelegate next, ILogger<RequestTelemetryMiddleware> logger, IOptions<RequestTelemetryOptions> options)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RequestTelemetryMiddleware> _logger = logger;
    private readonly RequestTelemetryOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var process = Process.GetCurrentProcess();
        var cpuBefore = process.TotalProcessorTime;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Response-Time-Ms"] = stopwatch.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value ?? "/"))
        using (LogContext.PushProperty("QueryString", context.Request.QueryString.Value ?? string.Empty))
        using (LogContext.PushProperty("RemoteIp", context.Connection.RemoteIpAddress?.ToString()))
        using (LogContext.PushProperty("UserAgent", context.Request.Headers.UserAgent.ToString()))
        using (LogContext.PushProperty("UserId", context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value))
        using (LogContext.PushProperty("UserRole", context.User.FindFirst("role")?.Value ?? context.User.FindFirst(ClaimTypes.Role)?.Value))
        {
            Exception? exception = null;
            var wasCanceled = false;

            try
            {
                await _next(context);
            }
            catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
            {
                exception = ex;
                wasCanceled = true;
                throw;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();

                var cpuAfter = process.TotalProcessorTime;
                var cpuUsedMs = (cpuAfter - cpuBefore).TotalMilliseconds;
                var workingSetMb = process.WorkingSet64 / (1024d * 1024d);
                var endpointName = context.GetEndpoint()?.DisplayName ?? "unknown";
                var statusCode = wasCanceled
                    ? 499
                    : exception is null
                        ? context.Response.StatusCode
                        : StatusCodes.Status500InternalServerError;
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                var logLevel = wasCanceled ? LogLevel.Warning : GetLogLevel(statusCode, elapsedMs);

                if (exception is null)
                {
                    _logger.Log(
                        logLevel,
                        "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms (Endpoint: {Endpoint}, CpuMs: {CpuMs}, WorkingSetMb: {WorkingSetMb:0.00})",
                        context.Request.Method,
                        context.Request.Path,
                        statusCode,
                        elapsedMs,
                        endpointName,
                        cpuUsedMs,
                        workingSetMb);
                }
                else if (wasCanceled)
                {
                    _logger.Log(
                        logLevel,
                        exception,
                        "HTTP {Method} {Path} canceled with {StatusCode} in {ElapsedMs} ms (Endpoint: {Endpoint}, CpuMs: {CpuMs}, WorkingSetMb: {WorkingSetMb:0.00})",
                        context.Request.Method,
                        context.Request.Path,
                        statusCode,
                        elapsedMs,
                        endpointName,
                        cpuUsedMs,
                        workingSetMb);
                }
                else
                {
                    _logger.Log(
                        logLevel,
                        exception,
                        "HTTP {Method} {Path} failed with {StatusCode} in {ElapsedMs} ms (Endpoint: {Endpoint}, CpuMs: {CpuMs}, WorkingSetMb: {WorkingSetMb:0.00})",
                        context.Request.Method,
                        context.Request.Path,
                        statusCode,
                        elapsedMs,
                        endpointName,
                        cpuUsedMs,
                        workingSetMb);
                }
            }
        }
    }

    private LogLevel GetLogLevel(int statusCode, long elapsedMs)
    {
        if (statusCode >= 500)
        {
            return LogLevel.Error;
        }

        if (statusCode >= 400 || elapsedMs >= _options.SlowRequestThresholdMs)
        {
            return LogLevel.Warning;
        }

        return LogLevel.Information;
    }

    private bool ShouldSkip(PathString path)
    {
        var requestPath = path.Value ?? string.Empty;

        return _options.ExcludedPaths.Any(excludedPath =>
            requestPath.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
    }
}
