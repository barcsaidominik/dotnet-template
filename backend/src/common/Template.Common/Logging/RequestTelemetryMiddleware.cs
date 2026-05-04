using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace Template.Common.Logging;

public sealed class RequestTelemetryMiddleware(RequestDelegate next, ILogger<RequestTelemetryMiddleware> logger, IOptions<RequestTelemetryOptions> options)
{
    private static readonly Process _currentProcess = Process.GetCurrentProcess();
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
        var cpuBefore = _currentProcess.TotalProcessorTime;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Response-Time-Ms"] = stopwatch.ElapsedMilliseconds.ToString();
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        using (LogContext.PushProperty("RequestMethod", SanitizeLogValue(context.Request.Method)))
        using (LogContext.PushProperty("RequestPath", SanitizeLogValue(context.Request.Path.Value ?? "/")))
        using (LogContext.PushProperty("QueryString", RedactQueryString(context.Request.QueryString.Value)))
        using (LogContext.PushProperty("RemoteIp", context.Connection.RemoteIpAddress?.ToString()))
        using (LogContext.PushProperty("UserAgent", SanitizeLogValue(context.Request.Headers.UserAgent.ToString())))
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

                var cpuAfter = _currentProcess.TotalProcessorTime;
                var cpuUsedMs = (cpuAfter - cpuBefore).TotalMilliseconds;
                var workingSetMb = _currentProcess.WorkingSet64 / (1024d * 1024d);
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
                        SanitizeLogValue(context.Request.Method),
                        SanitizeLogValue(context.Request.Path.Value),
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
                        SanitizeLogValue(context.Request.Method),
                        SanitizeLogValue(context.Request.Path.Value),
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
                        SanitizeLogValue(context.Request.Method),
                        SanitizeLogValue(context.Request.Path.Value),
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

    private static string RedactQueryString(string? queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return string.Empty;
        }

        var parsedQuery = QueryHelpers.ParseQuery(queryString);
        if (parsedQuery.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            "&",
            parsedQuery.Select(pair =>
            {
                var value = IsSensitiveQueryKey(pair.Key)
                    ? "[REDACTED]"
                    : string.Join(",", pair.Value.Select(static value => value ?? string.Empty));

                return $"{pair.Key}={value}";
            }));
    }

    private static bool IsSensitiveQueryKey(string key)
    {
        return key.Equals("token", StringComparison.OrdinalIgnoreCase)
            || key.Equals("refreshToken", StringComparison.OrdinalIgnoreCase)
            || key.Equals("access_token", StringComparison.OrdinalIgnoreCase)
            || key.Equals("code", StringComparison.OrdinalIgnoreCase)
            || key.Equals("password", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeLogValue(string? value)
    {
        return (value ?? string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
    }
}
