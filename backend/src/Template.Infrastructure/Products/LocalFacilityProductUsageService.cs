using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Infrastructure.Products;

public sealed class LocalFacilityProductUsageService(IEntityStore<Product> store) : IFacilityProductUsageService
{
    private readonly IEntityStore<Product> _store = store;

    public async Task<int> GetProductCountAsync(Guid facilityId, CancellationToken ct = default)
    {
        return await _store.GetQuery(asNoTracking: true, skipGuards: true)
            .CountAsync(product => product.FacilityId == facilityId, ct);
    }
}
