using Astriology.Domain.Entities;
using Astriology.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astriology.Infrastructure.Persistence.Configurations;

internal sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts", table =>
        {
            table.HasCheckConstraint("CK_Posts_Period", "[PeriodEnd] >= [PeriodStart]");
            table.HasCheckConstraint("CK_Posts_ViewCount", "[ViewCount] >= 0");
        });

        builder.HasKey(post => post.Id);

        builder.Property(post => post.Title).IsRequired().HasMaxLength(200);
        builder.Property(post => post.Summary).HasMaxLength(500);
        builder.Property(post => post.Content).IsRequired();
        builder.Property(post => post.ImageUrl).HasMaxLength(500);
        builder.Property(post => post.ViewCount).HasDefaultValue(0);
        builder.Property(post => post.IsPublished).HasDefaultValue(true);

        // Serves the category + sign filter and the current-period lookup, which are
        // the two queries every reader page runs.
        builder.HasIndex(post => new { post.CategoryId, post.ZodiacSignId, post.PeriodStart })
            .HasDatabaseName("IX_Posts_Category_Sign_PeriodStart");

        // The author is an Identity user. The domain deliberately holds no navigation
        // to it, so the relationship is declared with no navigation on either side.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(post => post.AuthorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Posts_AspNetUsers_AuthorId");

        // Cascade: a comment has no meaning without the post it belongs to.
        builder.HasMany(post => post.Comments)
            .WithOne(comment => comment.Post)
            .HasForeignKey(comment => comment.PostId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Comments_Posts");

        builder.Navigation(post => post.Comments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
