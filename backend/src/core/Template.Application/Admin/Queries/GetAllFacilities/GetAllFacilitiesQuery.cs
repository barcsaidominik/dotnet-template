using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Admin.Queries.GetAllFacilities;

public sealed record GetAllFacilitiesQuery : IRequest<ErrorOr<IReadOnlyList<FacilityDto>>>;
