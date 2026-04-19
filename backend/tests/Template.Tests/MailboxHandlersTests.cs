using ErrorOr;
using FluentAssertions;
using NSubstitute;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Application.Mailbox.Commands.MarkMailboxMessageAsRead;
using Template.Application.Mailbox.Queries.GetUnreadMailboxCount;

namespace Template.Tests;

public class MailboxHandlersTests
{
    private readonly IMailboxService _mailboxService;
    private readonly ICurrentUserService _currentUserService;

    public MailboxHandlersTests()
    {
        _mailboxService = Substitute.For<IMailboxService>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task GetUnreadMailboxCountQueryHandler_WithCurrentUser_ReturnsUnreadCount()
    {
        // Arrange
        const int unreadCount = 7;
        _mailboxService
            .GetUnreadCountAsync(_currentUserService.UserId, Arg.Any<CancellationToken>())
            .Returns(unreadCount);

        var handler = new GetUnreadMailboxCountQueryHandler(_mailboxService, _currentUserService);

        // Act
        var result = await handler.Handle(new GetUnreadMailboxCountQuery(), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeEquivalentTo(new MailboxUnreadCountDto(unreadCount));
        await _mailboxService.Received(1).GetUnreadCountAsync(_currentUserService.UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkMailboxMessageAsReadCommandHandler_WithCurrentUser_DelegatesToMailboxService()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        _mailboxService
            .MarkAsReadAsync(_currentUserService.UserId, messageId, Arg.Any<CancellationToken>())
            .Returns(Result.Updated);

        var handler = new MarkMailboxMessageAsReadCommandHandler(_mailboxService, _currentUserService);

        // Act
        var result = await handler.Handle(new MarkMailboxMessageAsReadCommand(messageId), CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(Result.Updated);
        await _mailboxService.Received(1).MarkAsReadAsync(_currentUserService.UserId, messageId, Arg.Any<CancellationToken>());
    }
}
