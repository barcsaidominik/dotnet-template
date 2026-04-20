using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Auth.Commands.Refresh;

public sealed record RefreshCommand(string RefreshToken) : IRequest<ErrorOr<LoginResult>>;
