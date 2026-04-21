using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Template.Application.Common.Interfaces;

namespace Template.Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User?.FindFirstValue("sub"), out var id)
                ? id
                : Guid.Empty;

    public Guid? FacilityId =>
        Guid.TryParse(User?.FindFirstValue("facilityId"), out var id)
            ? id
            : null;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public string? Email =>
        User?.FindFirstValue("email")
        ?? User?.FindFirstValue(ClaimTypes.Email);
}
