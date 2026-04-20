using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Queries.GetFacilityProductCount;

internal sealed class GetFacilityProductCountQueryHandler(IEntityStore<Product> store)
    : IQueryHandler<GetFacilityProductCountQuery, ErrorOr<int>>
{
    private readonly IEntityStore<Product> _store = store;

    public async ValueTask<ErrorOr<int>> Handle(GetFacilityProductCountQuery query, CancellationToken ct)
    {
        var count = await _store
            .GetQuery(asNoTracking: true, skipGuards: true)
            .Where(p => p.FacilityId == query.FacilityId)
            .CountAsync(ct);

        return count;
    }
}
