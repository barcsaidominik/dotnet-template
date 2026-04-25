using Mediator;
using Template.Domain.Entities;

namespace Template.Application.Products.Queries.GetProducts;

public sealed record GetProductsWithSearchQuery(string SearchTerm) : IRequest<IEnumerable<Product>>;
