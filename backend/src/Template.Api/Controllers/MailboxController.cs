using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Api.Extensions;
using Template.Application.Common.Dtos;
using Template.Application.Mailbox.Commands.MarkAllMailboxMessagesAsRead;
using Template.Application.Mailbox.Commands.MarkMailboxMessageAsRead;
using Template.Application.Mailbox.Queries.GetMailboxMessages;
using Template.Application.Mailbox.Queries.GetUnreadMailboxCount;

namespace Template.Api.Controllers;

[Route("api/[controller]")]
[Authorize]
public sealed class MailboxController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MailboxMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages()
    {
        return await SendAsync(new GetMailboxMessagesQuery()).ToActionResultAsync();
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(MailboxUnreadCountDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount()
    {
        return await SendAsync(new GetUnreadMailboxCountQuery()).ToActionResultAsync();
    }

    [HttpPost("{messageId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid messageId)
    {
        return await SendAsync(new MarkMailboxMessageAsReadCommand(messageId))
            .ToActionResultAsync(_ => NoContent());
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        return await SendAsync(new MarkAllMailboxMessagesAsReadCommand())
            .ToActionResultAsync(_ => NoContent());
    }
}
