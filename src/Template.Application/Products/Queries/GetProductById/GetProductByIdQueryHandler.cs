using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ErrorOr<Product>>
{
    private readonly IEntityStore<Product> _store;

    public GetProductByIdQueryHandler(IEntityStore<Product> store)
        => _store = store;

    public async ValueTask<ErrorOr<Product>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _store.GetQuery()
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
            return ProductErrors.NotFound;

        return product;
    }
}
