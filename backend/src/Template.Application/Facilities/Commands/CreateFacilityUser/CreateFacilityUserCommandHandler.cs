using ErrorOr;
using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Template.Application.Common;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Application.Common.Notifications;

namespace Template.Application.Facilities.Commands.CreateFacilityUser;

public sealed class CreateFacilityUserCommandHandler(
    IAuthService authService,
    INotificationService notificationService,
    IFrontendSettings frontendSettings,
    IMemoryCache cache,
    ILogger<CreateFacilityUserCommandHandler> logger) : IRequestHandler<CreateFacilityUserCommand, ErrorOr<CreateUserResult>>
{
    private readonly IAuthService _authService = authService;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IFrontendSettings _frontendSettings = frontendSettings;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<CreateFacilityUserCommandHandler> _logger = logger;

    public async ValueTask<ErrorOr<CreateUserResult>> Handle(CreateFacilityUserCommand request, CancellationToken ct)
    {
        var result = await _authService.CreateFacilityUserAsync(request.Email, request.FacilityId, request.Role, ct);

        if (result.IsError)
        {
            return result;
        }

        _cache.Remove(CacheKeys.ALL_USERS);

        try
        {
            var setupLink = $"{_frontendSettings.BaseUrl}/auth/set-password?token={Uri.EscapeDataString(result.Value.SetupToken)}&email={Uri.EscapeDataString(request.Email)}";
            var notification = new NotificationRequest(
                NotificationTemplateKey.SetupInvitation,
                new NotificationRecipient(request.Email),
                new SetupInvitationNotificationModel(setupLink),
                null,
                [NotificationChannelType.Email]);

            await _notificationService.SendAsync(notification, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send setup email to {Email}", request.Email);
        }

        return result;
    }
}
