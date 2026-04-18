using ErrorOr;
using Mediator;

namespace Template.Application.Admin.Commands.ApproveUser;

public sealed record ApproveUserCommand(Guid UserId, Guid FacilityId, string Role) : IRequest<ErrorOr<Success>>;
