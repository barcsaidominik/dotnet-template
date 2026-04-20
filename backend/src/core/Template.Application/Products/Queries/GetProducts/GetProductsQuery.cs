using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Domain.Entities;

namespace Template.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery(int Page = 1, int PageSize = 20) : IRequest<ErrorOr<PagedResult<Product>>>;
