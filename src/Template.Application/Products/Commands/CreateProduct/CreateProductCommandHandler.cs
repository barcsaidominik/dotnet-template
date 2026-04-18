using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;

namespace Template.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(IEntityStore<Product> store, ICurrentUserService currentUser) : IRequestHandler<CreateProductCommand, ErrorOr<Guid>>
{
    private readonly IEntityStore<Product> _store = store;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async ValueTask<ErrorOr<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        if (!_currentUser.FacilityId.HasValue)
        {
            return Error.Forbidden("Product.NoFacility", "User is not assigned to a facility");
        }

        var productResult = Product.Create(request.Name, request.Price, _currentUser.FacilityId.Value);
        if (productResult.IsError)
        {
            return productResult.Errors;
        }

        await _store.AddAsync(productResult.Value, ct);
        await _store.SaveChangesAsync(ct);

        return productResult.Value.Id;
    }
}
