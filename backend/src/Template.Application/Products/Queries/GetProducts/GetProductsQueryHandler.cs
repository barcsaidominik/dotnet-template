using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IEntityStore<Product> store) : IRequestHandler<GetProductsQuery, ErrorOr<IReadOnlyList<Product>>>
{
    private readonly IEntityStore<Product> _store = store;

    public async ValueTask<ErrorOr<IReadOnlyList<Product>>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var products = await _store.GetQuery(asNoTracking: true)
            .ToListAsync(ct);

        return products.AsReadOnly();
    }
}
