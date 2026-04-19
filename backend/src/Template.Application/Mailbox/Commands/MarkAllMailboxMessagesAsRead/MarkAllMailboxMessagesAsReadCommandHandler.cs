using ErrorOr;
using Mediator;
using Template.Application.Common.Interfaces;

namespace Template.Application.Mailbox.Commands.MarkAllMailboxMessagesAsRead;

public sealed class MarkAllMailboxMessagesAsReadCommandHandler(
    IMailboxService mailboxService,
    ICurrentUserService currentUserService) : IRequestHandler<MarkAllMailboxMessagesAsReadCommand, ErrorOr<Updated>>
{
    private readonly IMailboxService _mailboxService = mailboxService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async ValueTask<ErrorOr<Updated>> Handle(MarkAllMailboxMessagesAsReadCommand request, CancellationToken ct)
    {
        return await _mailboxService.MarkAllAsReadAsync(_currentUserService.UserId, ct);
    }
}
