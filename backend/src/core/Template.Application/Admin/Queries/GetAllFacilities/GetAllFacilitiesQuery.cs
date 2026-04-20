using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Admin.Queries.GetAllFacilities;

public sealed record GetAllFacilitiesQuery(
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<ErrorOr<IReadOnlyList<FacilityWithCountDto>>>;
