using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Mailbox.Commands.MarkMailboxMessageAsRead;

public sealed class MarkMailboxMessageAsReadCommandHandler(
    IMailboxService mailboxService,
    ICurrentUserService currentUserService) : IRequestHandler<MarkMailboxMessageAsReadCommand, ErrorOr<Updated>>
{
    private readonly IMailboxService _mailboxService = mailboxService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async ValueTask<ErrorOr<Updated>> Handle(MarkMailboxMessageAsReadCommand request, CancellationToken ct)
    {
        return await _mailboxService.MarkAsReadAsync(_currentUserService.UserId, request.MessageId, ct);
    }
}
