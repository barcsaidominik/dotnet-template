using ErrorOr;
using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Template.Application.Common;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Admin.Queries.GetAllUsers;

public sealed class GetAllUsersQueryHandler(IAuthService authService, IMemoryCache cache) : IRequestHandler<GetAllUsersQuery, ErrorOr<IReadOnlyList<UserDto>>>
{
    private readonly IAuthService _authService = authService;
    private readonly IMemoryCache _cache = cache;

    public async ValueTask<ErrorOr<IReadOnlyList<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        IReadOnlyList<UserDto>? users;

        if (_cache.TryGetValue(CacheKeys.ALL_USERS, out IReadOnlyList<UserDto>? cached))
        {
            users = cached!;
        }
        else
        {
            var result = await _authService.GetAllUsersAsync(ct);
            if (result.IsError)
            {
                return result;
            }

            users = result.Value;
            _cache.Set(CacheKeys.ALL_USERS, users, TimeSpan.FromMinutes(5));
        }

        var filtered = users.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLowerInvariant();
            filtered = filtered.Where(u => u.Email.ToLowerInvariant().Contains(searchLower));
        }

        filtered = ApplySorting(filtered, request.SortBy, request.SortDescending);

        return filtered.ToList().AsReadOnly();
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
