using Astriology.Domain.Entities;
using Astriology.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Astriology.Infrastructure.Persistence;

/// <summary>
/// The single database context: Identity's seven tables plus the five tables this
/// application owns.
/// </summary>
public sealed class AstriologyDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public AstriologyDbContext(DbContextOptions<AstriologyDbContext> options)
        : base(options)
    {
    }

    public DbSet<ZodiacSign> ZodiacSigns => Set<ZodiacSign>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Post> Posts => Set<Post>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity's own mappings must be applied first; the configurations below
        // then refine AspNetUsers with this application's columns.
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AstriologyDbContext).Assembly);
    }
}
