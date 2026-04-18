using ErrorOr;
using Mediator;

namespace Template.Application.Facilities.Commands.UpdateFacilityUserRole;

public sealed record UpdateFacilityUserRoleCommand(Guid FacilityId, Guid UserId, string NewRole) : IRequest<ErrorOr<Success>>;
