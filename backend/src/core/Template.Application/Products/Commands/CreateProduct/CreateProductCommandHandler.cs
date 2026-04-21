using ErrorOr;
using Mediator;
using Microsoft.Extensions.Caching.Memory;
using Template.Application.Common;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(IEntityStore<Product> store, ICurrentUserService currentUser, IMemoryCache cache) : IRequestHandler<CreateProductCommand, ErrorOr<Guid>>
{
    private readonly IEntityStore<Product> _store = store;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IMemoryCache _cache = cache;

    public async ValueTask<ErrorOr<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        if (!_currentUser.FacilityId.HasValue)
        {
            return Error.Forbidden("Product.NoFacility", "User is not assigned to a facility");
        }

        var productResult = Product.Create(request.Name, request.Price, _currentUser.FacilityId.Value, request.Quantity);
        if (productResult.IsError)
        {
            return productResult.Errors;
        }

        await _store.AddAsync(productResult.Value, ct);
        await _store.SaveChangesAsync(ct);

        _cache.Remove(CacheKeys.FacilityProducts(_currentUser.FacilityId.Value));

        return productResult.Value.Id;
    }
}
