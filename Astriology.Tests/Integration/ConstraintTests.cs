using Astriology.Domain.Entities;
using Astriology.Infrastructure.Persistence;
using Astriology.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Astriology.Tests.Integration;

/// <summary>
/// Proves the database enforces the rules the application relies on. These are the
/// tests that would silently pass against an in-memory provider, which is why the
/// suite runs against real SQL Server.
/// </summary>
[Collection(TestDatabaseCollection.Name)]
public sealed class ConstraintTests
{
    private readonly TestDatabaseFixture _fixture;

    public ConstraintTests(TestDatabaseFixture fixture) => _fixture = fixture;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Duplicate_zodiac_sign_slug_is_rejected()
    {
        await using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();

        var existingSlug = await context.ZodiacSigns.Select(sign => sign.Slug).FirstAsync(Token);

        // OrderNo is unique too, so use a free one to be sure the slug is what fails.
        context.ZodiacSigns.Add(new ZodiacSign(
            name: $"Kopya {Guid.NewGuid():N}",
            slug: existingSlug,
            element: "Ateş",
            orderNo: 12,
            startMonth: 3,
            startDay: 21,
            endMonth: 4,
            endDay: 20));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Token));
    }

    [Fact]
    public async Task Duplicate_category_slug_is_rejected()
    {
        await using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();

        var existingSlug = await context.Categories.Select(category => category.Slug).FirstAsync(Token);

        context.Categories.Add(new Category($"Kopya {Guid.NewGuid():N}", existingSlug));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Token));
    }

    [Fact]
    public async Task Deleting_a_post_also_deletes_its_comments()
    {
        int postId;
        int commentId;

        await using (var scope = _fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();
            var (signId, categoryId, authorId) = await ReferencesAsync(context);

            var post = new Post(
                title: $"Cascade testi {Guid.NewGuid():N}",
                content: "Silme davranışını doğrulamak için oluşturuldu.",
                zodiacSignId: signId,
                categoryId: categoryId,
                authorId: authorId,
                periodStart: DateTime.Now,
                periodEnd: DateTime.Now.AddDays(1),
                publishDate: DateTime.Now);

            context.Posts.Add(post);
            await context.SaveChangesAsync(Token);

            var comment = new Comment(post.Id, authorId, "Silinmesi beklenen yorum.");
            context.Comments.Add(comment);
            await context.SaveChangesAsync(Token);

            postId = post.Id;
            commentId = comment.Id;
        }

        await using (var scope = _fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();
            var post = await context.Posts.SingleAsync(candidate => candidate.Id == postId, Token);

            context.Posts.Remove(post);
            await context.SaveChangesAsync(Token);
        }

        await using (var scope = _fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();

            Assert.False(await context.Posts.AnyAsync(candidate => candidate.Id == postId, Token));
            Assert.False(await context.Comments.AnyAsync(candidate => candidate.Id == commentId, Token));
        }
    }

    [Fact]
    public async Task Deleting_a_category_that_still_has_posts_is_rejected()
    {
        await using var scope = _fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AstriologyDbContext>();

        // Any seeded category qualifies: every one of them has twelve posts.
        var categoryInUse = await context.Categories
            .FirstAsync(category => category.Posts.Any(), Token);

        context.Categories.Remove(categoryInUse);

        // Restrict on Posts.CategoryId is what the service layer turns into a
        // 409 Conflict in Faz 3 instead of letting a 500 escape.
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Token));
    }

    private static async Task<(int SignId, int CategoryId, int AuthorId)> ReferencesAsync(
        AstriologyDbContext context)
    {
        var signId = await context.ZodiacSigns.Select(sign => sign.Id).FirstAsync(Token);
        var categoryId = await context.Categories.Select(category => category.Id).FirstAsync(Token);
        var authorId = await context.Users.Select(user => user.Id).FirstAsync(Token);

        return (signId, categoryId, authorId);
    }
}
