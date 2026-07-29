namespace Astriology.Domain.Entities;

/// <summary>
/// One of the twelve zodiac signs. This is reference data: rows are created by
/// seeding and are never edited or removed, so the type deliberately exposes no
/// mutation methods.
/// </summary>
public sealed class ZodiacSign : BaseEntity
{
    /// <summary>Total number of signs. <see cref="OrderNo"/> runs from 1 to this value.</summary>
    public const int SignCount = 12;

    private readonly List<Post> _posts = [];

    /// <summary>Required by EF Core for materialization.</summary>
    private ZodiacSign()
    {
        Name = null!;
        Slug = null!;
        Element = null!;
    }

    public ZodiacSign(
        string name,
        string slug,
        string element,
        int orderNo,
        int startMonth,
        int startDay,
        int endMonth,
        int endDay,
        string? rulingPlanet = null,
        string? iconUrl = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(element);
        ArgumentOutOfRangeException.ThrowIfLessThan(orderNo, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(orderNo, SignCount);
        ValidateMonthDay(startMonth, startDay, nameof(startMonth), nameof(startDay));
        ValidateMonthDay(endMonth, endDay, nameof(endMonth), nameof(endDay));

        Name = name;
        Slug = slug;
        Element = element;
        OrderNo = orderNo;
        StartMonth = startMonth;
        StartDay = startDay;
        EndMonth = endMonth;
        EndDay = endDay;
        RulingPlanet = rulingPlanet;
        IconUrl = iconUrl;
        Description = description;
    }

    /// <summary>Display name in Turkish, for example "Koç".</summary>
    public string Name { get; private set; }

    /// <summary>URL-safe identifier without Turkish characters, for example "koc".</summary>
    public string Slug { get; private set; }

    /// <summary>Ateş, Toprak, Hava or Su.</summary>
    public string Element { get; private set; }

    public string? RulingPlanet { get; private set; }

    /// <summary>
    /// Position in the zodiac, 1 for Koç through 12 for Balık. The ascendant
    /// calculation steps through signs using this value, so it must stay accurate.
    /// </summary>
    public int OrderNo { get; private set; }

    public int StartMonth { get; private set; }

    public int StartDay { get; private set; }

    public int EndMonth { get; private set; }

    public int EndDay { get; private set; }

    public string? IconUrl { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Horoscope posts written for this sign.</summary>
    public IReadOnlyCollection<Post> Posts => _posts;

    private static void ValidateMonthDay(int month, int day, string monthParamName, string dayParamName)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(monthParamName, month, "Month must be between 1 and 12.");
        }

        if (day is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(dayParamName, day, "Day must be between 1 and 31.");
        }
    }
}
