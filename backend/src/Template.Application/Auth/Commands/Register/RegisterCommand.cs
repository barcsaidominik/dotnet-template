using ErrorOr;
using Mediator;

namespace Template.Application.Auth.Commands.Register;

public sealed record RegisterCommand(string Email, string Password) : IRequest<ErrorOr<Success>>;
