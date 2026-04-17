using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ErrorOr<Guid>>
{
    private readonly IProductRepository _repository;

    public CreateProductCommandHandler(IProductRepository repository)
        => _repository = repository;

    public async ValueTask<ErrorOr<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var productResult = Product.Create(request.Name, request.Price);
        if (productResult.IsError)
            return productResult.Errors;

        await _repository.AddAsync(productResult.Value, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return productResult.Value.Id;
    }
}
