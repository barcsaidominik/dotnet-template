using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Template.Domain.Entities;

namespace Template.Infrastructure.Persistence.Configurations;

public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditLog");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseIdentityAlwaysColumn();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(36).IsRequired();
        builder.Property(a => a.Action).IsRequired();
        builder.Property(a => a.UserEmail).HasMaxLength(256);
        builder.Property(a => a.ChangesJson).HasColumnType("text");
        builder.Property(a => a.OccurredAt).IsRequired();

        builder.HasIndex(a => new { a.EntityType, a.EntityId }).HasDatabaseName("IX_AuditLog_EntityType_EntityId");
        builder.HasIndex(a => a.UserId).HasDatabaseName("IX_AuditLog_UserId");
        builder.HasIndex(a => a.OccurredAt).HasDatabaseName("IX_AuditLog_OccurredAt");
    }
}
