namespace Template.Application.Common.Dtos;

public sealed record MailboxMessageDto(
    Guid Id,
    string Category,
    string TitleKey,
    string BodyKey,
    IReadOnlyDictionary<string, string> Parameters,
    string? Link,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAtUtc);
