using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Template.Infrastructure.Identity;
using Template.Infrastructure.Mailbox;
using Template.Infrastructure.Persistence;

namespace Template.Tests;

public class MailboxServiceTests
{
    [Fact]
    public async Task AddAsync_WithRecipient_PersistsMailboxMessage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager();
        var service = new MailboxService(dbContext, userManager);
        var recipientUserId = Guid.NewGuid();
        Dictionary<string, string> parameters = new()
        {
            ["createdBy"] = "admin@template.io"
        };

        // Act
        await service.AddAsync(
            recipientUserId,
            "user.created",
            "mailbox.title",
            "mailbox.body",
            parameters,
            "/admin/users",
            ct);

        // Assert
        var message = await dbContext.MailboxMessages.SingleAsync(ct);
        message.RecipientUserId.Should().Be(recipientUserId);
        message.Category.Should().Be("user.created");
        message.TitleKey.Should().Be("mailbox.title");
        message.BodyKey.Should().Be("mailbox.body");
        message.Link.Should().Be("/admin/users");
        message.GetParameters().Should().BeEquivalentTo(parameters);
    }

    [Fact]
    public async Task GetMessagesAsync_WithMultipleMessages_ReturnsNewestFirstAndLimitedShape()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager();
        var service = new MailboxService(dbContext, userManager);
        var userId = Guid.NewGuid();

        var olderMessage = Template.Domain.Entities.MailboxMessage.Create(
            userId,
            "older",
            "title.older",
            "body.older");
        var newerMessage = Template.Domain.Entities.MailboxMessage.Create(
            userId,
            "newer",
            "title.newer",
            "body.newer");

        dbContext.MailboxMessages.AddRange(olderMessage, newerMessage);
        await dbContext.SaveChangesAsync(ct);

        // Act
        var result = await service.GetMessagesAsync(userId, ct);

        // Assert
        result.Should().HaveCount(2);
        result[0].CreatedAt.Should().BeOnOrAfter(result[1].CreatedAt);
        result.Select(message => message.Category).Should().ContainInOrder("newer", "older");
    }

    [Fact]
    public async Task MarkAsReadAsync_WithOwnedMessage_MarksMessageAsRead()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager();
        var service = new MailboxService(dbContext, userManager);
        var userId = Guid.NewGuid();
        var message = Template.Domain.Entities.MailboxMessage.Create(
            userId,
            "mailbox.category",
            "mailbox.title",
            "mailbox.body");

        dbContext.MailboxMessages.Add(message);
        await dbContext.SaveChangesAsync(ct);

        // Act
        var result = await service.MarkAsReadAsync(userId, message.Id, ct);

        // Assert
        result.IsError.Should().BeFalse();
        var persistedMessage = await dbContext.MailboxMessages.SingleAsync(ct);
        persistedMessage.IsRead.Should().BeTrue();
        persistedMessage.ReadAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsReadAsync_WithDifferentUser_ReturnsNotFound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager();
        var service = new MailboxService(dbContext, userManager);
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var message = Template.Domain.Entities.MailboxMessage.Create(
            ownerUserId,
            "mailbox.category",
            "mailbox.title",
            "mailbox.body");

        dbContext.MailboxMessages.Add(message);
        await dbContext.SaveChangesAsync(ct);

        // Act
        var result = await service.MarkAsReadAsync(otherUserId, message.Id, ct);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorOr.ErrorType.NotFound);
        (await dbContext.MailboxMessages.SingleAsync(ct)).IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WithUnreadMessages_MarksOnlyCurrentUsersMessages()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var dbContext = CreateDbContext();
        var userManager = CreateUserManager();
        var service = new MailboxService(dbContext, userManager);
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        dbContext.MailboxMessages.AddRange(
            Template.Domain.Entities.MailboxMessage.Create(userId, "one", "title.one", "body.one"),
            Template.Domain.Entities.MailboxMessage.Create(userId, "two", "title.two", "body.two"),
            Template.Domain.Entities.MailboxMessage.Create(otherUserId, "other", "title.other", "body.other"));
        await dbContext.SaveChangesAsync(ct);

        // Act
        await service.MarkAllAsReadAsync(userId, ct);

        // Assert
        var userMessages = await dbContext.MailboxMessages.Where(message => message.RecipientUserId == userId).ToListAsync(ct);
        var otherMessage = await dbContext.MailboxMessages.SingleAsync(message => message.RecipientUserId == otherUserId, ct);

        userMessages.Should().OnlyContain(message => message.IsRead);
        otherMessage.IsRead.Should().BeFalse();
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static UserManager<AppUser> CreateUserManager()
    {
        var store = Substitute.For<IUserStore<AppUser>>();

        return new UserManager<AppUser>(
            store,
            null!,
            null!,
            [],
            [],
            null!,
            null!,
            null!,
            null!);
    }
}
