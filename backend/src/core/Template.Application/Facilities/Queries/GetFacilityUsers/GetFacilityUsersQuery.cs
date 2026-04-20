using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Facilities.Queries.GetFacilityUsers;

public sealed record GetFacilityUsersQuery(
    Guid FacilityId,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = false) : IRequest<ErrorOr<IReadOnlyList<UserDto>>>;
