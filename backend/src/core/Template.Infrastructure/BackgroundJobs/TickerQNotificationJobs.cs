using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Template.Application.Common.Notifications;
using TickerQ.Utilities.Base;

namespace Template.Infrastructure.BackgroundJobs;

public sealed class TickerQNotificationJobs(
    ILogger<TickerQNotificationJobs> logger,
    IServiceScopeFactory serviceScopeFactory)
{
    private readonly ILogger<TickerQNotificationJobs> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    [TickerFunction("Notification.Send", maxConcurrency: 4)]
    public async Task SendNotificationAsync(
        TickerFunctionContext<NotificationRequest> context,
        CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        _logger.LogInformation(
            "Sending notification. JobId: {JobId}, TemplateKey: {TemplateKey}, Recipient: {Recipient}",
            context.Id,
            context.Request.TemplateKey,
            context.Request.Recipient.Email);

        var request = RehydrateModel(context.Request);
        await notificationService.SendAsync(request, cancellationToken);

        _logger.LogInformation(
            "Notification sent successfully. JobId: {JobId}",
            context.Id);
    }

    private static NotificationRequest RehydrateModel(NotificationRequest request)
    {
        if (request.Model is not JsonElement element)
        {
            return request;
        }

        object typedModel = request.TemplateKey switch
        {
            NotificationTemplateKey.SetupInvitation =>
                element.Deserialize<SetupInvitationNotificationModel>()
                ?? throw new InvalidOperationException("Failed to deserialize SetupInvitationNotificationModel."),
            NotificationTemplateKey.PasswordReset =>
                element.Deserialize<PasswordResetNotificationModel>()
                ?? throw new InvalidOperationException("Failed to deserialize PasswordResetNotificationModel."),
            _ => throw new NotSupportedException($"Unknown notification template key: {request.TemplateKey}")
        };

        return request with
        {
            Model = typedModel
        };
    }
}
