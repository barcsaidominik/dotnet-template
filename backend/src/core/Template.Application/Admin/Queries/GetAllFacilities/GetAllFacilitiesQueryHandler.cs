using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Admin.Queries.GetAllFacilities;

public sealed class GetAllFacilitiesQueryHandler(
    IEntityStore<Facility> store,
    IAuthService authService) : IRequestHandler<GetAllFacilitiesQuery, ErrorOr<IReadOnlyList<FacilityWithCountDto>>>
{
    private readonly IEntityStore<Facility> _store = store;
    private readonly IAuthService _authService = authService;

    public async ValueTask<ErrorOr<IReadOnlyList<FacilityWithCountDto>>> Handle(GetAllFacilitiesQuery request, CancellationToken ct)
    {
        var facilitiesQuery = _store.GetQuery(asNoTracking: true, skipGuards: true);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLowerInvariant();
            facilitiesQuery = facilitiesQuery.Where(f => f.Name.ToLower().Contains(searchLower));
        }

        var facilities = await facilitiesQuery.ToListAsync(ct);
        var userCounts = await _authService.GetUserCountsByFacilityAsync(ct);

        var result = facilities.Select(f => new FacilityWithCountDto(
            f.Id,
            f.Name,
            userCounts.GetValueOrDefault(f.Id, 0)));

        result = ApplySorting(result, request.SortBy, request.SortDescending);

        return result.ToList().AsReadOnly();
    }

    private static IEnumerable<FacilityWithCountDto> ApplySorting(IEnumerable<FacilityWithCountDto> facilities, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? facilities.OrderByDescending(f => f.Name) : facilities.OrderBy(f => f.Name),
            _ => facilities.OrderBy(f => f.Name)
        };
    }
}
