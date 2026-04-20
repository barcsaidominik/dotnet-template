using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;
using Template.Domain.Errors;

namespace Template.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(IEntityStore<Product> store) : IRequestHandler<UpdateProductCommand, ErrorOr<Updated>>
{
    private readonly IEntityStore<Product> _store = store;

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

        return Result.Updated;
    }
}
