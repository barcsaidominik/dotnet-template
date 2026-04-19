using ErrorOr;
using Mediator;

namespace Template.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(Guid Id, string Name, decimal Price) : IRequest<ErrorOr<Updated>>;
