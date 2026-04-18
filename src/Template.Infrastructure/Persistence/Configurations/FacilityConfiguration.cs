using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Template.Domain.Entities;

namespace Template.Infrastructure.Persistence.Configurations;

public class FacilityConfiguration : IEntityTypeConfiguration<Facility> {
    public void Configure(EntityTypeBuilder<Facility> builder) {
        builder.ToTable("Facilities");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.CreatedAt).IsRequired();
    }
}
