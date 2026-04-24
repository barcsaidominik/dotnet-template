using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;
using Template.Application.Common.Interfaces;
using Template.Application.Common.Notifications;

namespace Template.Application.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IAuthService authService,
    INotificationService notificationService,
    IFrontendSettings frontendSettings,
    ILogger<ForgotPasswordCommandHandler> logger,
    IBackgroundJobScheduler? backgroundJobScheduler = null) : IRequestHandler<ForgotPasswordCommand, ErrorOr<Success>>
{
    private readonly IAuthService _authService = authService;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IFrontendSettings _frontendSettings = frontendSettings;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger = logger;
    private readonly IBackgroundJobScheduler? _backgroundJobScheduler = backgroundJobScheduler;

    public async ValueTask<ErrorOr<Success>> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var token = await _authService.ForgotPasswordAsync(request.Email, ct);

        if (token is not null)
        {
            var resetLink = $"{_frontendSettings.BaseUrl}/auth/set-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(request.Email)}";
            var notification = new NotificationRequest(
                NotificationTemplateKey.PasswordReset,
                new NotificationRecipient(request.Email),
                new PasswordResetNotificationModel(resetLink),
                null,
                [NotificationChannelType.Email]);

            try
            {
                if (_backgroundJobScheduler is not null)
                {
                    await _backgroundJobScheduler.ScheduleNotificationAsync(notification, ct);
                }
                else
                {
                    await _notificationService.SendAsync(notification, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email for {Email}", request.Email);
            }
        }

        return Result.Success;
    }
}
