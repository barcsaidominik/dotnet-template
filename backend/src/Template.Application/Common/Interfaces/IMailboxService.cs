using ErrorOr;
using Template.Application.Common.Dtos;

namespace Template.Application.Common.Interfaces;

public interface IMailboxService
{
    Task AddAsync(
        Guid recipientUserId,
        string category,
        string titleKey,
        string bodyKey,
        IReadOnlyDictionary<string, string>? parameters = null,
        string? link = null,
        CancellationToken ct = default);

    Task AddToRoleAsync(
        string role,
        string category,
        string titleKey,
        string bodyKey,
        IReadOnlyDictionary<string, string>? parameters = null,
        string? link = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<MailboxMessageDto>> GetMessagesAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<ErrorOr<Updated>> MarkAsReadAsync(Guid userId, Guid messageId, CancellationToken ct = default);
    Task<Updated> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
}
