using ErrorOr;
using Mediator;

namespace Template.Application.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<ErrorOr<Success>>;
