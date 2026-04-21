using Microsoft.EntityFrameworkCore;
using Template.Application.Common.Dtos;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;
using Template.Domain.Enums;

namespace Template.Infrastructure.Persistence;

public sealed class AuditLogRepository(AppDbContext dbContext) : IAuditLogRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<PagedResult<AuditEntryDto>> GetPagedAsync(
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
        CancellationToken ct)
    {
        var query = _dbContext.AuditLog.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(a => a.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(action) && Enum.TryParse<AuditAction>(action, true, out var parsedAction))
        {
            query = query.Where(a => a.Action == parsedAction);
        }

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (from.HasValue)
        {
            query = query.Where(a => a.OccurredAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(a => a.OccurredAt <= to.Value);
        }

        query = ApplySorting(query, sortBy, sortDescending);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditEntryDto(
                a.Id,
                a.EntityType,
                a.EntityId,
                a.Action.ToString(),
                a.UserId,
                a.UserEmail,
                a.ChangesJson,
                a.OccurredAt))
            .ToListAsync(ct);

        return new PagedResult<AuditEntryDto>(items.AsReadOnly(), totalCount, page, pageSize);
    }

    private static IQueryable<AuditEntry> ApplySorting(IQueryable<AuditEntry> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "entitytype" => descending ? query.OrderByDescending(a => a.EntityType) : query.OrderBy(a => a.EntityType),
            "action" => descending ? query.OrderByDescending(a => a.Action) : query.OrderBy(a => a.Action),
            "useremail" => descending ? query.OrderByDescending(a => a.UserEmail) : query.OrderBy(a => a.UserEmail),
            _ => descending ? query.OrderByDescending(a => a.OccurredAt) : query.OrderBy(a => a.OccurredAt),
        };
    }
}
