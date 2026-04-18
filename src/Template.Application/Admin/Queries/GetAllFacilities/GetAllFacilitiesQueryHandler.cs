using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Admin.Queries.GetAllFacilities;

public sealed class GetAllFacilitiesQueryHandler(IEntityStore<Facility> store) : IRequestHandler<GetAllFacilitiesQuery, ErrorOr<IReadOnlyList<FacilityDto>>>
{
    private readonly IEntityStore<Facility> _store = store;

    public async ValueTask<ErrorOr<IReadOnlyList<FacilityDto>>> Handle(GetAllFacilitiesQuery request, CancellationToken ct)
    {
        var facilities = await _store.GetQuery(asNoTracking: true, skipGuards: true)
            .Select(f => new FacilityDto(f.Id, f.Name))
            .ToListAsync(ct);

        return facilities.AsReadOnly();
    }
}
