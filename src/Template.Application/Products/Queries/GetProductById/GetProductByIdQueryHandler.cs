using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ErrorOr<Product>>
{
    private readonly IProductRepository _repository;

    public GetProductByIdQueryHandler(IProductRepository repository)
        => _repository = repository;

    public async ValueTask<ErrorOr<Product>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
            return ProductErrors.NotFound;

        return product;
    }
}
