using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Guards;

public sealed class FacilityProductGuard(ICurrentUserService currentUser) : IQueryGuard<Product>
{
    private readonly ICurrentUserService _currentUser = currentUser;

    public IQueryable<Product> Apply(IQueryable<Product> query)
    {
        return _currentUser.FacilityId.HasValue
                ? query.Where(p => p.FacilityId == _currentUser.FacilityId.Value)
                : query;
    }
}
