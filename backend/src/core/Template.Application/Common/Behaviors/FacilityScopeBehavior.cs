using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;
using Template.Domain.Constants;
using Template.Domain.Errors;

namespace Template.Application.Common.Behaviors;

/// <summary>
/// Enforces facility scope on every request implementing <see cref="IFacilityScopedRequest"/>:
/// a caller may only address its own facility, <see cref="Roles.SYSTEM_ADMIN"/> may address any.
/// </summary>
public sealed class FacilityScopeBehavior<TMessage, TResponse>(ICurrentUserService currentUserService) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage, IFacilityScopedRequest
    where TResponse : IErrorOr
{
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken ct
    )
    {
        if (_currentUserService.Role != Roles.SYSTEM_ADMIN && _currentUserService.FacilityId != message.FacilityId)
        {
            return (dynamic)FacilityErrors.AccessDenied;
        }

        return await next(message, ct);
    }
}
