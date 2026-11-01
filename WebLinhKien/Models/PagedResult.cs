namespace WebLinhKien.Models;

public class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalItems)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
    }

    public IReadOnlyList<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalItems { get; }
    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
