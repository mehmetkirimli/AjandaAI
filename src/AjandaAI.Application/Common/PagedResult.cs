// Tüm liste endpoint'lerinin döndüğü sayfalı sonuç modelidir.
// TotalPages, HasPrevious ve HasNext Page/PageSize/TotalCount'tan hesaplanır.
// Page ve PageSize PageRequest'ten gelir, dolayısıyla her zaman >= 1'dir.

namespace AjandaAI.Application.Common;

public class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }

    public IReadOnlyList<T> Items { get; }

    public int TotalCount { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    /// <summary>Sayfa bilgisini koruyarak öğeleri başka bir tipe (entity → DTO) çevirir.</summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), TotalCount, Page, PageSize);
}
