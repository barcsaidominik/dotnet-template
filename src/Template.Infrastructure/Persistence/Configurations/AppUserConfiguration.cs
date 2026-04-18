using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Template.Infrastructure.Identity;

namespace Template.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.FacilityId).IsRequired(false);
        builder.HasOne(u => u.Facility)
            .WithMany()
            .HasForeignKey(u => u.FacilityId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        builder.HasIndex(u => u.FacilityId);
    }
}
