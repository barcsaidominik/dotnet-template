using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Mailbox.Queries.GetUnreadMailboxCount;

public sealed class GetUnreadMailboxCountQueryHandler(
    IMailboxService mailboxService,
    ICurrentUserService currentUserService) : IRequestHandler<GetUnreadMailboxCountQuery, ErrorOr<MailboxUnreadCountDto>>
{
    private readonly IMailboxService _mailboxService = mailboxService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async ValueTask<ErrorOr<MailboxUnreadCountDto>> Handle(GetUnreadMailboxCountQuery request, CancellationToken ct)
    {
        var unreadCount = await _mailboxService.GetUnreadCountAsync(_currentUserService.UserId, ct);
        return new MailboxUnreadCountDto(unreadCount);
    }
}
