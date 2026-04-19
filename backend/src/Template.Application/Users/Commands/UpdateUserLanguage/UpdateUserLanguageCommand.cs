using ErrorOr;
using Mediator;

namespace Template.Application.Users.Commands.UpdateUserLanguage;

public sealed record UpdateUserLanguageCommand(string Language) : IRequest<ErrorOr<Updated>>;
