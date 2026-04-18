using ErrorOr;
using Mediator;

namespace Template.Application.Admin.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId) : IRequest<ErrorOr<Success>>;
