using ErrorOr;
using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Template.Application.Common;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Application.Common.Notifications;
using Template.Domain.Constants;
using Template.Domain.Errors;

namespace Template.Application.Facilities.Commands.CreateFacilityUser;

public sealed class CreateFacilityUserCommandHandler(
    IAuthService authService,
    IMailboxService mailboxService,
    ICurrentUserService currentUserService,
    IFrontendSettings frontendSettings,
    IMemoryCache cache,
    ILogger<CreateFacilityUserCommandHandler> logger,
    INotificationService notificationService,
    IBackgroundJobScheduler? backgroundJobScheduler = null) : IRequestHandler<CreateFacilityUserCommand, ErrorOr<CreateUserResult>>
{
    private readonly IAuthService _authService = authService;
    private readonly IBackgroundJobScheduler? _backgroundJobScheduler = backgroundJobScheduler;
    private readonly IMailboxService _mailboxService = mailboxService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IFrontendSettings _frontendSettings = frontendSettings;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<CreateFacilityUserCommandHandler> _logger = logger;
    private readonly INotificationService _notificationService = notificationService;

    public async ValueTask<ErrorOr<CreateUserResult>> Handle(CreateFacilityUserCommand request, CancellationToken ct)
    {
        if (_currentUserService.Role != Roles.SYSTEM_ADMIN && _currentUserService.FacilityId != request.FacilityId)
        {
            return FacilityErrors.AccessDenied;
        }

        var result = await _authService.CreateFacilityUserAsync(request.Email, request.FacilityId, request.Role, ct);

        if (result.IsError)
        {
            return result;
        }

        _cache.Remove(CacheKeys.ALL_USERS);

        var setupLink = $"{_frontendSettings.BaseUrl}/auth/set-password?token={Uri.EscapeDataString(result.Value.SetupToken)}&email={Uri.EscapeDataString(request.Email)}";
        var notification = new NotificationRequest(
            NotificationTemplateKey.SetupInvitation,
            new NotificationRecipient(request.Email),
            new SetupInvitationNotificationModel(setupLink),
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
            _logger.LogError(ex, "Failed to send setup email for {Email}. User was created but will not receive setup link.", request.Email);
        }

        if (_currentUserService.Role == Roles.FACILITY_ADMIN)
        {
            await _mailboxService.AddToRoleAsync(
                Roles.SYSTEM_ADMIN,
                "FacilityUserCreated",
                "mailbox.events.facilityUserCreated.title",
                "mailbox.events.facilityUserCreated.body",
                new Dictionary<string, string>
                {
                    ["email"] = request.Email,
                    ["role"] = request.Role,
                    ["facilityId"] = request.FacilityId.ToString()
                },
                "/admin/users",
                ct);
        }

        return result;
    }
}
