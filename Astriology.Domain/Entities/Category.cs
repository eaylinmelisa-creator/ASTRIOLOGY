namespace Astriology.Domain.Entities;

/// <summary>
/// A horoscope period type: Günlük, Haftalık, Aylık or Yıllık. Seeded with four
/// rows and editable by an administrator afterwards.
/// </summary>
public sealed class Category : BaseEntity
{
    private readonly List<Post> _posts = [];

    /// <summary>Required by EF Core for materialization.</summary>
    private Category()
    {
        Name = null!;
        Slug = null!;
    }

    public Category(string name, string slug, int displayOrder = 0, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentOutOfRangeException.ThrowIfNegative(displayOrder);

        Name = name;
        Slug = slug;
        DisplayOrder = displayOrder;
        Description = description;
    }

    /// <summary>Display name in Turkish, for example "Günlük".</summary>
    public string Name { get; private set; }

    /// <summary>URL-safe identifier without Turkish characters, for example "gunluk".</summary>
    public string Slug { get; private set; }

    /// <summary>Ascending sort order used when listing categories.</summary>
    public int DisplayOrder { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Horoscope posts filed under this category.</summary>
    public IReadOnlyCollection<Post> Posts => _posts;

    /// <summary>Applies an administrator edit. Every field is replaced.</summary>
    public void Update(string name, string slug, int displayOrder, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentOutOfRangeException.ThrowIfNegative(displayOrder);

        Name = name;
        Slug = slug;
        DisplayOrder = displayOrder;
        Description = description;
    }
}
