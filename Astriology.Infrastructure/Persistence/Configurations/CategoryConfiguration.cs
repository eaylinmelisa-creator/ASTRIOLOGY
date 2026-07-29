using Astriology.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astriology.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", table =>
        {
            table.HasCheckConstraint("CK_Categories_DisplayOrder", "[DisplayOrder] >= 0");
        });

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name).IsRequired().HasMaxLength(50);
        builder.Property(category => category.Slug).IsRequired().HasMaxLength(50);
        builder.Property(category => category.Description).HasMaxLength(300);
        builder.Property(category => category.DisplayOrder).HasDefaultValue(0);

        builder.HasIndex(category => category.Name).IsUnique().HasDatabaseName("UQ_Categories_Name");
        builder.HasIndex(category => category.Slug).IsUnique().HasDatabaseName("UQ_Categories_Slug");

        // Restrict is what turns "delete a category that still has posts" into the
        // 409 Conflict described in roadmap section 3.2 instead of silent data loss.
        builder.HasMany(category => category.Posts)
            .WithOne(post => post.Category)
            .HasForeignKey(post => post.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Posts_Categories");

        builder.Navigation(category => category.Posts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
