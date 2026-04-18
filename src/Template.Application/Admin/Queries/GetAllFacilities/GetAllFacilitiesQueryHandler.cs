using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Admin.Queries.GetAllFacilities;

public sealed class GetAllFacilitiesQueryHandler : IRequestHandler<GetAllFacilitiesQuery, ErrorOr<IReadOnlyList<FacilityDto>>> {
    private readonly IEntityStore<Facility> _store;

    public GetAllFacilitiesQueryHandler(IEntityStore<Facility> store)
        => _store = store;

    public async ValueTask<ErrorOr<IReadOnlyList<FacilityDto>>> Handle(GetAllFacilitiesQuery request, CancellationToken cancellationToken) {
        var facilities = await _store.GetQuery(asNoTracking: true, skipGuards: true)
            .Select(f => new FacilityDto(f.Id, f.Name))
            .ToListAsync(cancellationToken);

        return facilities.AsReadOnly();
    }
}
