using FluentAssertions;
using Template.Domain.Entities;

namespace Template.Tests;

public class MailboxMessageTests
{
    [Fact]
    public void Create_WithParametersAndLink_StoresUnreadMessage()
    {
        // Arrange
        var recipientUserId = Guid.NewGuid();
        Dictionary<string, string> parameters = new()
        {
            ["facilityName"] = "Central Facility",
            ["createdBy"] = "admin@template.io"
        };
        const string link = "/mailbox/123";

        // Act
        var message = MailboxMessage.Create(
            recipientUserId,
            "user.created",
            "mailbox.title",
            "mailbox.body",
            parameters,
            link);

        // Assert
        message.RecipientUserId.Should().Be(recipientUserId);
        message.Category.Should().Be("user.created");
        message.TitleKey.Should().Be("mailbox.title");
        message.BodyKey.Should().Be("mailbox.body");
        message.Link.Should().Be(link);
        message.IsRead.Should().BeFalse();
        message.ReadAtUtc.Should().BeNull();
        message.GetParameters().Should().BeEquivalentTo(parameters);
    }

    [Fact]
    public void GetParameters_WithNoParameters_ReturnsEmptyDictionary()
    {
        // Arrange
        var message = MailboxMessage.Create(
            Guid.NewGuid(),
            "system.info",
            "mailbox.title",
            "mailbox.body");

        // Act
        var parameters = message.GetParameters();

        // Assert
        parameters.Should().BeEmpty();
    }

    [Fact]
    public void MarkAsRead_FirstCall_SetsReadStateAndTimestamp()
    {
        // Arrange
        var message = MailboxMessage.Create(
            Guid.NewGuid(),
            "system.info",
            "mailbox.title",
            "mailbox.body");

        // Act
        message.MarkAsRead();

        // Assert
        message.IsRead.Should().BeTrue();
        message.ReadAtUtc.Should().NotBeNull();
        message.ReadAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsRead_SecondCall_DoesNotOverwriteExistingTimestamp()
    {
        // Arrange
        var message = MailboxMessage.Create(
            Guid.NewGuid(),
            "system.info",
            "mailbox.title",
            "mailbox.body");

        message.MarkAsRead();
        var firstReadAt = message.ReadAtUtc;

        // Act
        message.MarkAsRead();

        // Assert
        message.IsRead.Should().BeTrue();
        message.ReadAtUtc.Should().Be(firstReadAt);
    }
}
