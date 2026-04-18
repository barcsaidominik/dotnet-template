using ErrorOr;
using Mediator;

namespace Template.Application.Auth.Commands.SetPassword;

public sealed record SetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<ErrorOr<Success>>;
