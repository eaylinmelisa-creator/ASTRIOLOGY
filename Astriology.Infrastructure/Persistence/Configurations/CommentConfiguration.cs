using Astriology.Domain.Entities;
using Astriology.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astriology.Infrastructure.Persistence.Configurations;

internal sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Content).IsRequired().HasMaxLength(1000);

        // Comments are always read for one post in chronological order.
        builder.HasIndex(comment => new { comment.PostId, comment.CreatedAt })
            .HasDatabaseName("IX_Comments_Post_CreatedAt");

        // Restrict, not cascade: users are soft-deleted, so their comments must survive
        // and be shown as "Silinmiş kullanıcı".
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(comment => comment.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Comments_AspNetUsers_UserId");

        // The Comment -> Post relationship is configured from the principal side in
        // PostConfiguration.
    }
}
