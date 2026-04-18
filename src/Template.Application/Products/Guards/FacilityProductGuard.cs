using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Guards;

public sealed class FacilityProductGuard : IQueryGuard<Product>
{
    private readonly ICurrentUserService _currentUser;

    public FacilityProductGuard(ICurrentUserService currentUser)
        => _currentUser = currentUser;

    public IQueryable<Product> Apply(IQueryable<Product> query)
        => _currentUser.FacilityId.HasValue
            ? query.Where(p => p.FacilityId == _currentUser.FacilityId.Value)
            : query;
}
