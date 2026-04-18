using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Template.Application.Common.Interfaces;

namespace Template.Infrastructure.Identity;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User?.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    public Guid? FacilityId =>
        Guid.TryParse(User?.FindFirstValue("facilityId"), out var id) ? id : null;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
