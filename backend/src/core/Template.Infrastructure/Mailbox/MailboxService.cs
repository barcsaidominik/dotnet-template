using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;
using Template.Infrastructure.Identity;
using Template.Infrastructure.Persistence;

namespace Template.Infrastructure.Mailbox;

public sealed class MailboxService(
    AppDbContext dbContext,
    UserManager<AppUser> userManager) : IMailboxService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly UserManager<AppUser> _userManager = userManager;

    public async Task AddAsync(
        Guid recipientUserId,
        string category,
        string titleKey,
        string bodyKey,
        IReadOnlyDictionary<string, string>? parameters = null,
        string? link = null,
        CancellationToken ct = default)
    {
        var message = MailboxMessage.Create(recipientUserId, category, titleKey, bodyKey, parameters, link);
        await _dbContext.MailboxMessages.AddAsync(message, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task AddToRoleAsync(
        string role,
        string category,
        string titleKey,
        string bodyKey,
        IReadOnlyDictionary<string, string>? parameters = null,
        string? link = null,
        CancellationToken ct = default)
    {
        var users = await _userManager.GetUsersInRoleAsync(role);
        if (users.Count == 0)
        {
            return;
        }

        var messages = users
            .Select(user => MailboxMessage.Create(user.Id, category, titleKey, bodyKey, parameters, link))
            .ToArray();

        await _dbContext.MailboxMessages.AddRangeAsync(messages, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<MailboxMessageDto>> GetMessagesAsync(Guid userId, CancellationToken ct = default)
    {
        var messages = await _dbContext.MailboxMessages
            .AsNoTracking()
            .Where(message => message.RecipientUserId == userId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return messages
            .Select(message => new MailboxMessageDto(
                message.Id,
                message.Category,
                message.TitleKey,
                message.BodyKey,
                message.GetParameters(),
                message.Link,
                message.IsRead,
                message.CreatedAt,
                message.ReadAtUtc))
            .ToList();
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return _dbContext.MailboxMessages
            .AsNoTracking()
            .CountAsync(message => message.RecipientUserId == userId && !message.IsRead, ct);
    }

    public async Task<ErrorOr<Updated>> MarkAsReadAsync(Guid userId, Guid messageId, CancellationToken ct = default)
    {
        var message = await _dbContext.MailboxMessages
            .SingleOrDefaultAsync(x => x.Id == messageId && x.RecipientUserId == userId, ct);

        if (message is null)
        {
            return Error.NotFound("Mailbox.NotFound");
        }

        message.MarkAsRead();
        await _dbContext.SaveChangesAsync(ct);
        return Result.Updated;
    }

    public async Task<Updated> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await _dbContext.MailboxMessages
            .Where(message => message.RecipientUserId == userId && !message.IsRead)
            .ToListAsync(ct);

        foreach (var message in unread)
        {
            message.MarkAsRead();
        }

        await _dbContext.SaveChangesAsync(ct);
        return Result.Updated;
    }
}
