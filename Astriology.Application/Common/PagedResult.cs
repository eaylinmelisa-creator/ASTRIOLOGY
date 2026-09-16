namespace Astriology.Application.Common;

/// <summary>
/// One page of a larger collection, together with the totals a client needs to
/// render pagination controls.
/// </summary>
/// <remarks>
/// Every collection endpoint returns this rather than a bare list, so no endpoint
/// can accidentally fetch an unbounded result set.
/// </remarks>
public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Items { get; }

    /// <summary>Rows matching the query across every page, not just this one.</summary>
    public int TotalCount { get; }

    /// <summary>One-based page number.</summary>
    public int Page { get; }

    public int PageSize { get; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Empty(int page, int pageSize) => new([], 0, page, pageSize);
}
