using ErrorOr;
using Mediator;
using Template.Application.Common.Dtos;

namespace Template.Application.Mailbox.Queries.GetUnreadMailboxCount;

public sealed record GetUnreadMailboxCountQuery : IRequest<ErrorOr<MailboxUnreadCountDto>>;
