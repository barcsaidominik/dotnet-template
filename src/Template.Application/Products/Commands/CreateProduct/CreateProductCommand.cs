using ErrorOr;
using Mediator;

namespace Template.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(string Name, decimal Price) : IRequest<ErrorOr<Guid>>;
