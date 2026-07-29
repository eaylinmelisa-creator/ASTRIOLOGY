using Astriology.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astriology.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // The table name and Identity's own columns come from IdentityDbContext;
        // only this application's additions are configured here.
        builder.Property(user => user.BirthPlace).HasMaxLength(100);
        builder.Property(user => user.IsActive).HasDefaultValue(true);

        // DateOnly and TimeOnly map to date and time, so no time component is stored
        // for a birth date and no date component for a birth time.
        builder.Property(user => user.BirthDate).HasColumnType("date");
        builder.Property(user => user.BirthTime).HasColumnType("time");

        // Restrict on both: the twelve signs are reference data and must never be
        // removable, and a user's calculated signs must not disappear silently.
        builder.HasOne(user => user.AscendantSign)
            .WithMany()
            .HasForeignKey(user => user.AscendantSignId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AspNetUsers_ZodiacSigns_AscendantSignId");

        builder.HasOne(user => user.SunSign)
            .WithMany()
            .HasForeignKey(user => user.SunSignId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AspNetUsers_ZodiacSigns_SunSignId");
    }
}
