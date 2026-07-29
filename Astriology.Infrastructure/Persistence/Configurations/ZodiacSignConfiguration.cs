using Astriology.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astriology.Infrastructure.Persistence.Configurations;

internal sealed class ZodiacSignConfiguration : IEntityTypeConfiguration<ZodiacSign>
{
    public void Configure(EntityTypeBuilder<ZodiacSign> builder)
    {
        // The check constraints mirror the invariants the entity constructor enforces.
        // Keeping them in both places means bad data cannot arrive through a path that
        // bypasses the domain, such as a manual script or a future bulk import.
        builder.ToTable("ZodiacSigns", table =>
        {
            table.HasCheckConstraint("CK_ZodiacSigns_OrderNo", "[OrderNo] BETWEEN 1 AND 12");
            table.HasCheckConstraint("CK_ZodiacSigns_StartMonth", "[StartMonth] BETWEEN 1 AND 12");
            table.HasCheckConstraint("CK_ZodiacSigns_EndMonth", "[EndMonth] BETWEEN 1 AND 12");
            table.HasCheckConstraint("CK_ZodiacSigns_StartDay", "[StartDay] BETWEEN 1 AND 31");
            table.HasCheckConstraint("CK_ZodiacSigns_EndDay", "[EndDay] BETWEEN 1 AND 31");
        });

        builder.HasKey(sign => sign.Id);

        builder.Property(sign => sign.Name).IsRequired().HasMaxLength(50);
        builder.Property(sign => sign.Slug).IsRequired().HasMaxLength(50);
        builder.Property(sign => sign.Element).IsRequired().HasMaxLength(20);
        builder.Property(sign => sign.RulingPlanet).HasMaxLength(50);
        builder.Property(sign => sign.IconUrl).HasMaxLength(300);
        builder.Property(sign => sign.Description).HasMaxLength(1000);

        builder.HasIndex(sign => sign.Name).IsUnique().HasDatabaseName("UQ_ZodiacSigns_Name");
        builder.HasIndex(sign => sign.Slug).IsUnique().HasDatabaseName("UQ_ZodiacSigns_Slug");
        builder.HasIndex(sign => sign.OrderNo).IsUnique().HasDatabaseName("UQ_ZodiacSigns_OrderNo");

        builder.HasMany(sign => sign.Posts)
            .WithOne(post => post.ZodiacSign)
            .HasForeignKey(post => post.ZodiacSignId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Posts_ZodiacSigns");

        // Posts is exposed as IReadOnlyCollection over a private _posts list, so EF
        // must write through the field rather than the property.
        builder.Navigation(sign => sign.Posts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
