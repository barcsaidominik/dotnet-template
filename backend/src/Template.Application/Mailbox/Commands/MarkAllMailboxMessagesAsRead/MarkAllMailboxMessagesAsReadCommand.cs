using ErrorOr;
using Mediator;

namespace Template.Application.Mailbox.Commands.MarkAllMailboxMessagesAsRead;

public sealed record MarkAllMailboxMessagesAsReadCommand : IRequest<ErrorOr<Updated>>;
