using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Template.Application.Common.Interfaces;
using Template.Domain.Entities;
using Template.Domain.Enums;

namespace Template.Infrastructure.Persistence;

public sealed class AuditInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    private static readonly HashSet<string> _auditedTypes = [nameof(Facility), nameof(Product)];
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

    private void ApplyAudit(DbContext? context)
    {
        if (context is not AppDbContext appDbContext)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var currentUser = scope.ServiceProvider.GetService<ICurrentUserService>();

        var userId = currentUser?.IsAuthenticated == true ? currentUser.UserId : (Guid?)null;
        var userEmail = currentUser?.IsAuthenticated == true ? currentUser.Email : null;

        var entries = appDbContext.ChangeTracker.Entries()
            .Where(e => _auditedTypes.Contains(e.Entity.GetType().Name)
                && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType().Name;
            var entityId = entry.Property("Id").CurrentValue?.ToString() ?? string.Empty;
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Modified => AuditAction.Updated,
                EntityState.Deleted => AuditAction.Deleted,
                _ => AuditAction.Updated
            };

            string? changesJson = null;
            if (entry.State == EntityState.Added)
            {
                var snapshot = entry.Properties
                    .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                changesJson = JsonSerializer.Serialize(snapshot, _jsonOptions);
            }
            else if (entry.State == EntityState.Modified)
            {
                var changes = entry.Properties
                    .Where(p => p.IsModified)
                    .Select(p => new { Property = p.Metadata.Name, Before = p.OriginalValue, After = p.CurrentValue })
                    .ToList();
                changesJson = JsonSerializer.Serialize(changes, _jsonOptions);
            }
            else if (entry.State == EntityState.Deleted)
            {
                var snapshot = entry.Properties
                    .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                changesJson = JsonSerializer.Serialize(snapshot, _jsonOptions);
            }

            var auditEntry = AuditEntry.Create(entityType, entityId, action, userId, userEmail, changesJson);
            appDbContext.AuditLog.Add(auditEntry);
        }
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
