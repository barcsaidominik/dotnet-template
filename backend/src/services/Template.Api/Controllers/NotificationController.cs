using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Application.Common.Interfaces;
using Template.Common.Controllers;

namespace Template.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationController(ISender sender, IWebhookService webhookService) : ApiController(sender)
{
    [HttpPost("webhook-test")]
    public async Task<IActionResult> TestWebhook(
        [FromBody] WebhookTestRequest request,
        CancellationToken ct)
    {
        await webhookService.NotifyAsync(request.WebhookUrl, new
        {
            test = true
        }, ct);
        return Ok();
    }
}

public record WebhookTestRequest(string WebhookUrl);
