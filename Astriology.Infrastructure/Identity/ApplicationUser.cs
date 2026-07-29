using Astriology.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Astriology.Infrastructure.Identity;

/// <summary>
/// Application user, extending Identity with the birth details the horoscope
/// features need.
/// </summary>
/// <remarks>
/// Unlike the domain entities this type uses plain settable properties. Identity's
/// <c>UserManager</c> creates and mutates users through its own pipeline and cannot
/// call custom behaviour methods, so private setters would simply be bypassed.
/// </remarks>
public sealed class ApplicationUser : IdentityUser<int>
{
    /// <summary>Required at registration; the sun sign is derived from it.</summary>
    public DateOnly BirthDate { get; set; }

    /// <summary>Optional. Without it the ascendant cannot be calculated.</summary>
    public TimeOnly? BirthTime { get; set; }

    /// <summary>Informational only - the ascendant calculation ignores coordinates.</summary>
    public string? BirthPlace { get; set; }

    public int? AscendantSignId { get; set; }

    public int? SunSignId { get; set; }

    /// <summary>
    /// Soft-delete flag. A deleted user keeps their row and their comments, but
    /// cannot sign in and is shown as "Silinmiş kullanıcı".
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Local time; see roadmap section 5.9.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ZodiacSign? AscendantSign { get; set; }

    public ZodiacSign? SunSign { get; set; }
}
