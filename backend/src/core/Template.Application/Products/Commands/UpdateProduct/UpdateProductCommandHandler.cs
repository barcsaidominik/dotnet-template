using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Template.Application.Common;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(IEntityStore<Product> store, IMemoryCache cache) : IRequestHandler<UpdateProductCommand, ErrorOr<Updated>>
{
    private readonly IEntityStore<Product> _store = store;
    private readonly IMemoryCache _cache = cache;

    public async ValueTask<ErrorOr<Updated>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _store.GetQuery()
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        var updateResult = product.Update(request.Name, request.Price, request.Quantity);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await _store.SaveChangesAsync(ct);

        _cache.Remove(CacheKeys.FacilityProducts(product.FacilityId));

        return Result.Updated;
    }
}
