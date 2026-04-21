using ErrorOr;
using Mediator;

namespace Template.Application.Admin.Commands.UpdateUser;

public sealed record UpdateUserCommand(Guid UserId, string Email) : IRequest<ErrorOr<Updated>>;
