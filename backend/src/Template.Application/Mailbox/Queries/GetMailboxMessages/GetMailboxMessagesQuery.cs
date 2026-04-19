using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Mailbox.Queries.GetMailboxMessages;

public sealed record GetMailboxMessagesQuery : IRequest<ErrorOr<IReadOnlyList<MailboxMessageDto>>>;
