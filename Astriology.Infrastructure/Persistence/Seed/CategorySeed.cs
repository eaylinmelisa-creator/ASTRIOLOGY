using Astriology.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Astriology.Infrastructure.Persistence.Seed;

/// <summary>
/// Inserts the four period types. Unlike signs these are editable by an
/// administrator afterwards, so the guard checks only whether the table is empty.
/// </summary>
internal static class CategorySeed
{
    public static async Task SeedAsync(AstriologyDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        context.Categories.AddRange(Build());
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<Category> Build() =>
    [
        new("Günlük", "gunluk", 1, "Bugüne özel burç yorumu."),
        new("Haftalık", "haftalik", 2, "İçinde bulunduğunuz haftanın genel görünümü."),
        new("Aylık", "aylik", 3, "Ayın tamamını kapsayan burç yorumu."),
        new("Yıllık", "yillik", 4, "Yılın genel eğilimleri ve dönüm noktaları."),
    ];
}
