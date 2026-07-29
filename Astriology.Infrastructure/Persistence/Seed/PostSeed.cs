using Astriology.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Astriology.Infrastructure.Persistence.Seed;

/// <summary>
/// Creates one post per sign and category - twelve signs times four categories,
/// forty-eight rows in total.
/// </summary>
/// <remarks>
/// Periods are calculated from the current date so a freshly seeded database always
/// has a reading that covers today. Without that, /api/posts/current would fall back
/// to the "latest published" branch on every request right after setup.
/// </remarks>
internal static class PostSeed
{
    public static async Task SeedAsync(
        AstriologyDbContext context,
        int authorId,
        CancellationToken cancellationToken)
    {
        if (await context.Posts.AnyAsync(cancellationToken))
        {
            return;
        }

        var signs = await context.ZodiacSigns
            .OrderBy(sign => sign.OrderNo)
            .ToListAsync(cancellationToken);

        var categories = await context.Categories
            .OrderBy(category => category.DisplayOrder)
            .ToListAsync(cancellationToken);

        var now = DateTime.Now;
        var posts = new List<Post>(signs.Count * categories.Count);

        foreach (var category in categories)
        {
            var (periodStart, periodEnd) = ResolvePeriod(category.Slug, now);

            foreach (var sign in signs)
            {
                posts.Add(new Post(
                    title: $"{sign.Name} Burcu {category.Name} Yorumu",
                    content: BuildContent(sign, category.Slug),
                    zodiacSignId: sign.Id,
                    categoryId: category.Id,
                    authorId: authorId,
                    periodStart: periodStart,
                    periodEnd: periodEnd,
                    publishDate: periodStart,
                    summary: BuildSummary(sign, category.Slug)));
            }
        }

        context.Posts.AddRange(posts);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static (DateTime Start, DateTime End) ResolvePeriod(string categorySlug, DateTime now)
    {
        var today = now.Date;

        return categorySlug switch
        {
            "gunluk" => (today, EndOfDay(today)),
            "haftalik" => WeekContaining(today),
            "aylik" => MonthContaining(today),
            "yillik" => YearContaining(today),
            _ => throw new InvalidOperationException(
                $"No period rule defined for category slug '{categorySlug}'."),
        };
    }

    /// <summary>Last representable instant of the given day, so the period is inclusive.</summary>
    private static DateTime EndOfDay(DateTime day) => day.AddDays(1).AddTicks(-1);

    private static (DateTime Start, DateTime End) WeekContaining(DateTime today)
    {
        // DayOfWeek starts at Sunday; shift so the week runs Monday to Sunday.
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var monday = today.AddDays(-daysSinceMonday);

        return (monday, EndOfDay(monday.AddDays(6)));
    }

    private static (DateTime Start, DateTime End) MonthContaining(DateTime today)
    {
        var firstDay = new DateTime(today.Year, today.Month, 1);

        return (firstDay, EndOfDay(firstDay.AddMonths(1).AddDays(-1)));
    }

    private static (DateTime Start, DateTime End) YearContaining(DateTime today)
    {
        var firstDay = new DateTime(today.Year, 1, 1);

        return (firstDay, EndOfDay(new DateTime(today.Year, 12, 31)));
    }

    private static string BuildSummary(ZodiacSign sign, string categorySlug) => categorySlug switch
    {
        "gunluk" => $"{sign.Name} burcu için günün öne çıkan başlıkları ve dikkat edilmesi gerekenler.",
        "haftalik" => $"{sign.Name} burcunu bu hafta neler bekliyor? İş, ilişkiler ve sağlık başlıkları.",
        "aylik" => $"{sign.Name} burcu için ayın genel eğilimi ve dönüm noktaları.",
        "yillik" => $"{sign.Name} burcunun yıl boyunca karşılaşacağı fırsatlar ve zorluklar.",
        _ => $"{sign.Name} burcu yorumu.",
    };

    private static string BuildContent(ZodiacSign sign, string categorySlug)
    {
        var element = sign.Element;
        var planet = sign.RulingPlanet ?? "yönetici gezegenin";

        return categorySlug switch
        {
            "gunluk" =>
                $"{sign.Name} burcu için bugün {element} elementinin enerjisi belirgin şekilde hissediliyor. "
                + "Sabah saatlerinde başlayan işleri gün içinde bitirmek, akşama sarkıtmaktan daha verimli olacak.\n\n"
                + $"{planet} etkisi altında iletişim kanalların açık. Uzun süredir ertelediğin bir konuşmayı "
                + "bugün yapmak için uygun bir gün. Beklemediğin bir yerden gelen haber planlarını değiştirebilir; "
                + "esnek kalmakta fayda var.\n\n"
                + "Akşam saatlerinde dinlenmeye zaman ayır. Bugünün yorgunluğu yarına taşınmasın.",

            "haftalik" =>
                $"{sign.Name} burcu bu hafta {element} elementinin dengeleyici etkisiyle ilerliyor. "
                + "Haftanın ilk yarısı planlama, ikinci yarısı uygulama için daha uygun.\n\n"
                + $"İş hayatında {planet} kaynaklı bir ivme söz konusu. Ekip içinde üstlendiğin sorumluluk artabilir; "
                + "bunu yük değil görünürlük fırsatı olarak değerlendir. Maddi konularda acele karar vermekten kaçın.\n\n"
                + "İlişkilerde açık iletişim haftanın anahtarı. Söylenmeyen şeyler bu hafta daha çok yorar. "
                + "Hafta sonu kendine ayıracağın zaman, önümüzdeki haftaya güçlü başlamanı sağlayacak.",

            "aylik" =>
                $"{sign.Name} burcu için bu ay {element} elementinin temposu belirleyici oluyor. "
                + "Ayın ilk on günü yeni başlangıçlar, ortası düzeltme ve revizyon, sonu ise toparlanma dönemi.\n\n"
                + $"{planet} etkisi kariyer başlığını öne çıkarıyor. Uzun süredir üzerinde çalıştığın bir konunun "
                + "karşılığını bu ay alabilirsin. Eğitim, sertifika ya da yeni bir beceri edinmek için de elverişli bir dönem.\n\n"
                + "Sağlık tarafında düzenli uyku ve beslenme, ayın geri kalanındaki enerjini doğrudan etkileyecek. "
                + "Ay sonuna doğru sosyal çevrende genişleme mümkün; yeni tanışıklıklar ilerisi için kapı açabilir.",

            "yillik" =>
                $"{sign.Name} burcu için bu yıl, {element} elementinin doğasına uygun biçimde adım adım ilerleyerek "
                + "kalıcı sonuç alma yılı. Hızlı ama geçici kazanımlar yerine sabırla kurulan yapılar öne çıkıyor.\n\n"
                + $"Yılın ilk çeyreğinde {planet} etkisiyle netleşen bir hedef, yılın tamamına yön verecek. "
                + "İkinci çeyrek sınav niteliğinde; burada verdiğin kararlar sonbaharda karşına çıkacak.\n\n"
                + "Yılın ikinci yarısında ilişkiler ve ortaklıklar başlığı ağırlık kazanıyor. "
                + "Tek başına taşımaya çalıştığın yükleri paylaşmayı öğrendiğinde işler belirgin şekilde hafifleyecek.\n\n"
                + "Yıl sonuna doğru emeğinin karşılığını görüyorsun. Bu yıl senin için bir sıçrama değil, "
                + "sağlam bir zemin kurma yılı olacak.",

            _ => $"{sign.Name} burcu için hazırlanmış yorum.",
        };
    }
}
