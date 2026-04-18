using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<ErrorOr<LoginResult>>;
