using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Template.Common.Idempotency;

/// <summary>
/// Middleware that ensures idempotency for POST requests via the Idempotency-Key header.
/// Clients can safely retry POST requests with the same key without duplicate side effects.
/// Cache TTL: 24 hours.
/// </summary>
/// <remarks>
/// This is an in-memory implementation suitable for single-node deployments.
/// TODO: For multi-instance deployments, consider using Redis or another distributed cache.
/// </remarks>
public sealed class IdempotencyMiddleware(
    RequestDelegate next,
    IMemoryCache cache,
    ILogger<IdempotencyMiddleware> logger)
{
    private const string IDEMPOTENCY_KEY_HEADER = "Idempotency-Key";
    private const string REPLAYED_HEADER = "X-Idempotency-Replayed";
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromHours(24);

    private static readonly string[] _excludedPaths =
    [
        "/health",
        "/scalar",
        "/openapi"
    ];

    private readonly RequestDelegate _next = next;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<IdempotencyMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method) || ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(IDEMPOTENCY_KEY_HEADER, out var idempotencyKey) ||
            string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            return;
        }

        var cacheKey = $"idempotency:{idempotencyKey}";

        if (_cache.TryGetValue<CachedResponse>(cacheKey, out var cached) && cached is not null)
        {
            _logger.LogInformation(
                "Idempotency cache hit for key {IdempotencyKey}, returning cached response with status {StatusCode}",
                idempotencyKey.ToString(),
                cached.StatusCode);

            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            context.Response.Headers[REPLAYED_HEADER] = "true";
            await context.Response.WriteAsync(cached.Body);
            return;
        }

        var originalBody = context.Response.Body;
        using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        try
        {
            await _next(context);

            memStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memStream).ReadToEndAsync();

            var cachedResponse = new CachedResponse(
                context.Response.StatusCode,
                responseBody,
                context.Response.ContentType ?? "application/json");

            _cache.Set(cacheKey, cachedResponse, _cacheTtl);

            _logger.LogDebug(
                "Cached response for idempotency key {IdempotencyKey} with status {StatusCode}",
                idempotencyKey.ToString(),
                context.Response.StatusCode);

            memStream.Seek(0, SeekOrigin.Begin);
            await memStream.CopyToAsync(originalBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool ShouldSkip(PathString path)
    {
        var requestPath = path.Value ?? string.Empty;

        return Array.Exists(_excludedPaths, excludedPath =>
            requestPath.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record CachedResponse(int StatusCode, string Body, string ContentType);
}
