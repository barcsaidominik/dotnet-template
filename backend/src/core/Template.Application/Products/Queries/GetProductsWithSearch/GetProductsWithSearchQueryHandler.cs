using Mediator;
using Template.Application.Common.Interfaces;
using Template.Application.Products.Queries.GetProducts;
using Template.Domain.Entities;

namespace Template.Application.Products.Queries.GetProductsWithSearch;

public sealed class GetProductsWithSearchQueryHandler(IEntityStore<Product> store) : IRequestHandler<GetProductsWithSearchQuery, IEnumerable<Product>>
{
    private readonly IEntityStore<Product> _store = store;

    public async ValueTask<IEnumerable<Product>> Handle(GetProductsWithSearchQuery request, CancellationToken ct)
    {
        return await _store.SearchAsync(nameof(Product.Name), request.SearchTerm, ct);
    }
}
