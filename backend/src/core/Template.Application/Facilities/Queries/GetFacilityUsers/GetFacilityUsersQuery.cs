using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Facilities.Queries.GetFacilityUsers;

public sealed record GetFacilityUsersQuery(Guid FacilityId) : IRequest<ErrorOr<IReadOnlyList<UserDto>>>;
