using ErrorOr;
using Mediator;

namespace Template.Application.Mailbox.Commands.MarkMailboxMessageAsRead;

public sealed record MarkMailboxMessageAsReadCommand(Guid MessageId) : IRequest<ErrorOr<Updated>>;
