using ErrorOr;
using Mediator;

namespace Template.Application.Auth.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest<ErrorOr<Success>>;
