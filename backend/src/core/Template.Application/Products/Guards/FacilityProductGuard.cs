using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Guards;

public sealed class FacilityProductGuard(ICurrentUserService currentUser) : IQueryGuard<Product>
{
    private readonly ICurrentUserService _currentUser = currentUser;

    public IQueryable<Product> Apply(IQueryable<Product> query)
    {
        if (!_currentUser.FacilityId.HasValue)
        {
            return query.Where(_ => false);
        }

        return query.Where(p => p.FacilityId == _currentUser.FacilityId.Value);
    }
}
