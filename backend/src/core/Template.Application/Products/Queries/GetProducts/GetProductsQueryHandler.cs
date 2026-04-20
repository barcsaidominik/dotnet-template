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

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(searchLower));
        }

        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Product>(items.AsReadOnly(), totalCount, request.Page, request.PageSize);
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "price" => descending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "createdat" => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "name" => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "quantity" => descending ? query.OrderByDescending(p => p.Quantity) : query.OrderBy(p => p.Quantity),
            _ => query.OrderBy(p => p.Name)
        };
    }
}
