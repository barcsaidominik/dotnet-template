using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;

namespace Template.Application.Mailbox.Queries.GetMailboxMessages;

public sealed class GetMailboxMessagesQueryHandler(
    IMailboxService mailboxService,
    ICurrentUserService currentUserService) : IRequestHandler<GetMailboxMessagesQuery, ErrorOr<IReadOnlyList<MailboxMessageDto>>>
{
    private readonly IMailboxService _mailboxService = mailboxService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async ValueTask<ErrorOr<IReadOnlyList<MailboxMessageDto>>> Handle(GetMailboxMessagesQuery request, CancellationToken ct)
    {
        var messages = await _mailboxService.GetMessagesAsync(_currentUserService.UserId, ct);
        return ErrorOrFactory.From<IReadOnlyList<MailboxMessageDto>>(messages);
    }
}
