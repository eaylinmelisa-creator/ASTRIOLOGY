namespace Astriology.Domain.Entities;

/// <summary>
/// A horoscope reading written for one sign and one period type.
/// </summary>
/// <remarks>
/// The author is referenced by id only. Identity types live in the infrastructure
/// layer (roadmap section 3, "Identity İstisnası"), so a navigation property to the
/// user would force a framework reference into the domain. Author details are joined
/// in the repository instead.
/// </remarks>
public sealed class Post : BaseEntity
{
    private readonly List<Comment> _comments = [];

    /// <summary>Required by EF Core for materialization.</summary>
    private Post()
    {
        Title = null!;
        Content = null!;
        ZodiacSign = null!;
        Category = null!;
    }

    public Post(
        string title,
        string content,
        int zodiacSignId,
        int categoryId,
        int authorId,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime publishDate,
        string? summary = null,
        string? imageUrl = null,
        bool isPublished = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(zodiacSignId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(categoryId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(authorId);
        ValidatePeriod(periodStart, periodEnd);

        Title = title;
        Content = content;
        ZodiacSignId = zodiacSignId;
        CategoryId = categoryId;
        AuthorId = authorId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        PublishDate = publishDate;
        Summary = summary;
        ImageUrl = imageUrl;
        IsPublished = isPublished;
        ZodiacSign = null!;
        Category = null!;
    }

    public string Title { get; private set; }

    public string? Summary { get; private set; }

    /// <summary>Plain text. Line breaks are preserved and rendered as written.</summary>
    public string Content { get; private set; }

    /// <summary>Relative path of an uploaded image, for example "/uploads/xxx.jpg".</summary>
    public string? ImageUrl { get; private set; }

    public int ZodiacSignId { get; private set; }

    public int CategoryId { get; private set; }

    /// <summary>Identity user id of the author. Fixed at creation; edits never reassign it.</summary>
    public int AuthorId { get; private init; }

    /// <summary>First moment the reading applies to.</summary>
    public DateTime PeriodStart { get; private set; }

    /// <summary>Last moment the reading applies to. Never earlier than <see cref="PeriodStart"/>.</summary>
    public DateTime PeriodEnd { get; private set; }

    public DateTime PublishDate { get; private set; }

    /// <summary>Detail-page hit counter. Only ever increases.</summary>
    public int ViewCount { get; private set; }

    public bool IsPublished { get; private set; }

    /// <summary>Set on every edit; null means the post has never been edited.</summary>
    public DateTime? UpdatedAt { get; private set; }

    public ZodiacSign ZodiacSign { get; private set; }

    public Category Category { get; private set; }

    /// <summary>Reader comments, newest ordering applied by the query rather than here.</summary>
    public IReadOnlyCollection<Comment> Comments => _comments;

    /// <summary>
    /// Applies an administrator edit and stamps <see cref="UpdatedAt"/>. The author
    /// and the view counter are not editable and are left untouched.
    /// </summary>
    public void Update(
        string title,
        string content,
        int zodiacSignId,
        int categoryId,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime publishDate,
        string? summary,
        string? imageUrl,
        bool isPublished)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(zodiacSignId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(categoryId);
        ValidatePeriod(periodStart, periodEnd);

        Title = title;
        Content = content;
        ZodiacSignId = zodiacSignId;
        CategoryId = categoryId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        PublishDate = publishDate;
        Summary = summary;
        ImageUrl = imageUrl;
        IsPublished = isPublished;
        UpdatedAt = DateTime.Now;
    }

    /// <summary>
    /// Records one detail-page view. Reading a post is not an edit, so
    /// <see cref="UpdatedAt"/> stays untouched.
    /// </summary>
    public void IncrementViewCount() => ViewCount++;

    private static void ValidatePeriod(DateTime periodStart, DateTime periodEnd)
    {
        if (periodEnd < periodStart)
        {
            throw new ArgumentException(
                "PeriodEnd cannot be earlier than PeriodStart.",
                nameof(periodEnd));
        }
    }
}
