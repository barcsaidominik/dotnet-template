using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Facilities.Commands.RemoveFacilityUser;

public sealed record RemoveFacilityUserCommand(Guid FacilityId, Guid UserId) : IRequest<ErrorOr<Success>>, IFacilityScopedRequest;
