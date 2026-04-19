using ErrorOr;
using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Template.Application.Common;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Facilities.Commands.CreateFacilityUser;

public sealed class CreateFacilityUserCommandHandler(
    IAuthService authService,
    IEmailService emailService,
    IFrontendSettings frontendSettings,
    IMemoryCache cache,
    ILogger<CreateFacilityUserCommandHandler> logger) : IRequestHandler<CreateFacilityUserCommand, ErrorOr<CreateUserResult>>
{
    private readonly IAuthService _authService = authService;
    private readonly IEmailService _emailService = emailService;
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
            var setupLink = $"{_frontendSettings.BaseUrl}/setup?token={Uri.EscapeDataString(result.Value.SetupToken)}&email={Uri.EscapeDataString(request.Email)}";

            await _emailService.SendSetupEmailAsync(request.Email, setupLink, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send setup email to {Email}", request.Email);
        }

        return result;
    }
}
