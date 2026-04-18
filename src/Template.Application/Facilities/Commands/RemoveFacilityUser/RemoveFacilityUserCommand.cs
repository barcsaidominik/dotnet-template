using ErrorOr;
using Mediator;

namespace Template.Application.Facilities.Commands.RemoveFacilityUser;

public sealed record RemoveFacilityUserCommand(Guid FacilityId, Guid UserId) : IRequest<ErrorOr<Success>>;
