using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Constants;
using Template.Domain.Errors;

namespace Template.Application.Facilities.Queries.GetFacilityUsers;

public sealed class GetFacilityUsersQueryHandler(
    IAuthService authService,
    ICurrentUserService currentUserService) : IRequestHandler<GetFacilityUsersQuery, ErrorOr<IReadOnlyList<UserDto>>>
{
    private readonly IAuthService _authService = authService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async ValueTask<ErrorOr<IReadOnlyList<UserDto>>> Handle(GetFacilityUsersQuery request, CancellationToken ct)
    {
        if (_currentUserService.Role != Roles.SYSTEM_ADMIN && _currentUserService.FacilityId != request.FacilityId)
        {
            return FacilityErrors.AccessDenied;
        }

        var result = await _authService.GetFacilityUsersAsync(request.FacilityId, ct);
        if (result.IsError)
        {
            return result;
        }

        var users = result.Value.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLowerInvariant();
            users = users.Where(u => u.Email.ToLowerInvariant().Contains(searchLower));
        }

        users = ApplySorting(users, request.SortBy, request.SortDescending);

        return users.ToList().AsReadOnly();
    }

    private static IEnumerable<UserDto> ApplySorting(IEnumerable<UserDto> users, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "role" => descending ? users.OrderByDescending(u => u.Role) : users.OrderBy(u => u.Role),
            "email" => descending ? users.OrderByDescending(u => u.Email) : users.OrderBy(u => u.Email),
            _ => users.OrderBy(u => u.Email)
        };
    }
}
