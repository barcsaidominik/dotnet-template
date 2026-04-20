using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Users.Commands.UpdateUserLanguage;

public sealed class UpdateUserLanguageCommandHandler(
    IAuthService authService,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateUserLanguageCommand, ErrorOr<Updated>>
{
    private readonly IAuthService _authService = authService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async ValueTask<ErrorOr<Updated>> Handle(UpdateUserLanguageCommand request, CancellationToken ct)
    {
        return await _authService.UpdatePreferredLanguageAsync(_currentUserService.UserId, request.Language, ct);
    }
}
