using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Template.Application.Common;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IEntityStore<Product> store, ICurrentUserService currentUser, IMemoryCache cache) : IRequestHandler<GetProductsQuery, ErrorOr<PagedResult<Product>>>
{
    private readonly IEntityStore<Product> _store = store;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IMemoryCache _cache = cache;

    public async ValueTask<ErrorOr<PagedResult<Product>>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        if (!_currentUser.FacilityId.HasValue)
        {
            return new PagedResult<Product>([], 0, request.Page, request.PageSize);
        }

        var cacheKey = CacheKeys.FacilityProducts(_currentUser.FacilityId.Value);
        IReadOnlyList<Product> products;

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<Product>? cached))
        {
            products = cached!;
        }
        else
        {
            var allProducts = await _store.GetQuery(asNoTracking: true).ToListAsync(ct);
            products = allProducts.AsReadOnly();
            _cache.Set(cacheKey, products, TimeSpan.FromMinutes(5));
        }

        var filtered = products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLowerInvariant();
            filtered = filtered.Where(p => p.Name.ToLowerInvariant().Contains(searchLower));
        }

        filtered = ApplySorting(filtered, request.SortBy, request.SortDescending);

        var totalCount = filtered.Count();
        var items = filtered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResult<Product>(items.AsReadOnly(), totalCount, request.Page, request.PageSize);
    }

    private static IEnumerable<Product> ApplySorting(IEnumerable<Product> products, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "price" => descending ? products.OrderByDescending(p => p.Price) : products.OrderBy(p => p.Price),
            "createdat" => descending ? products.OrderByDescending(p => p.CreatedAt) : products.OrderBy(p => p.CreatedAt),
            "name" => descending ? products.OrderByDescending(p => p.Name) : products.OrderBy(p => p.Name),
            "quantity" => descending ? products.OrderByDescending(p => p.Quantity) : products.OrderBy(p => p.Quantity),
            _ => products.OrderBy(p => p.Name)
        };
    }
}
