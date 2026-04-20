using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IEntityStore<Product> store) : IRequestHandler<GetProductsQuery, ErrorOr<PagedResult<Product>>>
{
    private readonly IEntityStore<Product> _store = store;

    public async ValueTask<ErrorOr<PagedResult<Product>>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var query = _store.GetQuery(asNoTracking: true);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Product>(items.AsReadOnly(), totalCount, request.Page, request.PageSize);
    }
}
