namespace Template.Application.Common.Dtos;

public sealed record AuditEntryDto(
    long Id,
    string EntityType,
    string EntityId,
    string Action,
    Guid? UserId,
    string? UserEmail,
    string? ChangesJson,
    DateTime OccurredAt);
