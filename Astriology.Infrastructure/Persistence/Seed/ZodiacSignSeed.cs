using Astriology.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Astriology.Infrastructure.Persistence.Seed;

/// <summary>
/// Inserts the twelve signs. These are reference data and are never edited, so the
/// seed runs once and does nothing on later starts.
/// </summary>
/// <remarks>
/// The date ranges cover the calendar with no gaps and no overlaps: every sign starts
/// the day after the previous one ends. Oğlak crosses the year boundary and is stored
/// as a single row (22 December - 19 January), as decided in roadmap section 5.1.
/// </remarks>
internal static class ZodiacSignSeed
{
    public static async Task SeedAsync(AstriologyDbContext context, CancellationToken cancellationToken)
    {
        if (await context.ZodiacSigns.AnyAsync(cancellationToken))
        {
            return;
        }

        context.ZodiacSigns.AddRange(Build());
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<ZodiacSign> Build() =>
    [
        new("Koç", "koc", "Ateş", 1, 3, 21, 4, 20, "Mars",
            description: "Öncü, girişken ve cesur. Harekete geçmekte hiç tereddüt etmez."),
        new("Boğa", "boga", "Toprak", 2, 4, 21, 5, 21, "Venüs",
            description: "Sabırlı, kararlı ve konforuna düşkün. Güvenceyi her şeyin üstünde tutar."),
        new("İkizler", "ikizler", "Hava", 3, 5, 22, 6, 22, "Merkür",
            description: "Meraklı, iletişimi güçlü ve çok yönlü. Sıkılmaya tahammülü yoktur."),
        new("Yengeç", "yengec", "Su", 4, 6, 23, 7, 22, "Ay",
            description: "Duygusal, koruyucu ve aile odaklı. Sezgileri çoğu zaman mantığından önde gider."),
        new("Aslan", "aslan", "Ateş", 5, 7, 23, 8, 22, "Güneş",
            description: "Kendine güvenen, cömert ve sahne alan. Takdir edilmek ona iyi gelir."),
        new("Başak", "basak", "Toprak", 6, 8, 23, 9, 22, "Merkür",
            description: "Analitik, titiz ve hizmet odaklı. Ayrıntıyı kimsenin görmediği yerde görür."),
        new("Terazi", "terazi", "Hava", 7, 9, 23, 10, 22, "Venüs",
            description: "Dengeli, estetik ve uzlaşmacı. Adalet duygusu güçlüdür."),
        new("Akrep", "akrep", "Su", 8, 10, 23, 11, 21, "Plüton",
            description: "Tutkulu, derin ve kararlı. Yüzeyde kalan hiçbir şeyle yetinmez."),
        new("Yay", "yay", "Ateş", 9, 11, 22, 12, 21, "Jüpiter",
            description: "Özgür, iyimser ve maceracı. Ufku genişleten her şey ilgisini çeker."),
        new("Oğlak", "oglak", "Toprak", 10, 12, 22, 1, 19, "Satürn",
            description: "Disiplinli, hırslı ve sorumluluk sahibi. Uzun vadeli düşünür."),
        new("Kova", "kova", "Hava", 11, 1, 20, 2, 18, "Uranüs",
            description: "Özgün, yenilikçi ve bağımsız. Kalıpların dışında düşünür."),
        new("Balık", "balik", "Su", 12, 2, 19, 3, 20, "Neptün",
            description: "Sezgisel, şefkatli ve hayal gücü geniş. Sınırları bulanıklaştırır."),
    ];
}
