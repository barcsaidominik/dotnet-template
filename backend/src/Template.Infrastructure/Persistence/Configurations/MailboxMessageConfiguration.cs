using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Template.Domain.Entities;

namespace Template.Infrastructure.Persistence.Configurations;

public sealed class MailboxMessageConfiguration : IEntityTypeConfiguration<MailboxMessage>
{
    public void Configure(EntityTypeBuilder<MailboxMessage> builder)
    {
        builder.ToTable("MailboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.RecipientUserId).IsRequired();
        builder.Property(message => message.Category).HasMaxLength(100).IsRequired();
        builder.Property(message => message.TitleKey).HasMaxLength(200).IsRequired();
        builder.Property(message => message.BodyKey).HasMaxLength(200).IsRequired();
        builder.Property(message => message.ParametersJson);
        builder.Property(message => message.Link).HasMaxLength(500);
        builder.Property(message => message.IsRead).IsRequired();
        builder.Property(message => message.CreatedAt).IsRequired();
        builder.Property(message => message.ReadAtUtc);
        builder.HasIndex(message => new { message.RecipientUserId, message.IsRead, message.CreatedAt });
    }
}
