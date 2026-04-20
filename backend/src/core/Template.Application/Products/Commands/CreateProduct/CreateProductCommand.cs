using ErrorOr;
using Mediator;

namespace Template.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(string Name, decimal Price, int Quantity = 0) : IRequest<ErrorOr<Guid>>;
