using ErrorOr;
using Mediator;

namespace Template.Application.Products.Queries.GetFacilityProductCount;

public sealed record GetFacilityProductCountQuery(Guid FacilityId) : IQuery<ErrorOr<int>>;
