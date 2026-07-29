using Astriology.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astriology.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Token).IsRequired().HasMaxLength(500);

        builder.HasIndex(token => token.Token).IsUnique().HasDatabaseName("UQ_RefreshTokens_Token");

        // Cascade: a refresh token is meaningless without its user, and unlike comments
        // there is nothing to preserve for display.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RefreshTokens_AspNetUsers_UserId");
    }
}
