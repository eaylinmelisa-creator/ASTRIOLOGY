namespace Astriology.Domain.Entities;

/// <summary>
/// A reader comment on a post. Comments are published immediately; there is no
/// moderation queue and no nesting.
/// </summary>
/// <remarks>
/// Like <see cref="Post.AuthorId"/>, the commenter is referenced by id only so the
/// domain stays free of Identity types.
/// </remarks>
public sealed class Comment : BaseEntity
{
    /// <summary>Required by EF Core for materialization.</summary>
    private Comment()
    {
        Content = null!;
        Post = null!;
    }

    public Comment(int postId, int userId, string content)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(postId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        PostId = postId;
        UserId = userId;
        Content = content;
        Post = null!;
    }

    public int PostId { get; private init; }

    /// <summary>Identity user id of the commenter. Fixed at creation.</summary>
    public int UserId { get; private init; }

    public string Content { get; private set; }

    /// <summary>
    /// Set when the comment is edited; null means never edited. The UI shows a
    /// "düzenlendi" badge when this has a value.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    public Post Post { get; private set; }

    /// <summary>
    /// Replaces the comment text and stamps <see cref="UpdatedAt"/>. Only the
    /// comment's owner may reach this; that check belongs to the service layer.
    /// </summary>
    public void UpdateContent(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Content = content;
        UpdatedAt = DateTime.Now;
    }
}
