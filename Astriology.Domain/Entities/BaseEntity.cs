namespace Astriology.Domain.Entities;

/// <summary>
/// Shared identity and creation stamp for every persisted domain entity.
/// Both values are assigned once and are never changed afterwards.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Database-generated primary key. Stays zero until the entity is persisted.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// Creation time in local time. The project stores local time rather than UTC
    /// because it serves a single time zone; see roadmap section 5.9.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.Now;
}
