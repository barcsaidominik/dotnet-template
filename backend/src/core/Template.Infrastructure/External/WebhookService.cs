using System.Net.Http.Json;
using Template.Application.Common.Interfaces;

namespace Template.Infrastructure.External;

public sealed class WebhookService(IHttpClientFactory httpClientFactory)
    : IWebhookService
{
    public async Task NotifyAsync(string webhookUrl, object payload, CancellationToken ct = default)
    {
        using var client = httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(webhookUrl, payload, ct);
        response.EnsureSuccessStatusCode();
    }
}
