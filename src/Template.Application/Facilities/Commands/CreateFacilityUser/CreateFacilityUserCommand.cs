using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Facilities.Commands.CreateFacilityUser;

public sealed record CreateFacilityUserCommand(Guid FacilityId, string Email, string Role) : IRequest<ErrorOr<CreateUserResult>>;
