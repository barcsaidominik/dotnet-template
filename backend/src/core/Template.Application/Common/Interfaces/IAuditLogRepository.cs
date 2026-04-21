using Template.Application.Common.Dtos;

namespace Template.Application.Common.Interfaces;

public interface IAuditLogRepository
{
    Task<PagedResult<AuditEntryDto>> GetPagedAsync(
        string? entityType,
        string? entityId,
        string? action,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        string? sortBy,
        bool sortDescending,
        CancellationToken ct);
}
