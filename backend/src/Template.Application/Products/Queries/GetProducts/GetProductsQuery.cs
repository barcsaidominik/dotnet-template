using ErrorOr;
using Mediator;
using Template.Domain.Entities;

namespace Template.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery : IRequest<ErrorOr<IReadOnlyList<Product>>>;
