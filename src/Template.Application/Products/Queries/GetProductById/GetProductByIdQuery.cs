using ErrorOr;
using Mediator;
using Template.Domain.Entities;

namespace Template.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ErrorOr<Product>>;
