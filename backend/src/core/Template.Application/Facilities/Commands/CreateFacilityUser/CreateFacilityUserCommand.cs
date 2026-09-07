using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Facilities.Commands.CreateFacilityUser;

public sealed record CreateFacilityUserCommand(Guid FacilityId, string Email, string Role) : IRequest<ErrorOr<CreateUserResult>>, IFacilityScopedRequest;
