namespace Template.Application.Common.Interfaces;

public interface IWebhookService
{
    Task NotifyAsync(string webhookUrl, object payload, CancellationToken ct = default);
}
