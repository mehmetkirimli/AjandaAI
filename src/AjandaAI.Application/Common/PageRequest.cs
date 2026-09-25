// Liste endpoint'lerinin ortak sayfalama parametreleridir (?page=&pageSize=).
// Geçersiz değerler hata yerine sessizce sınıra çekilir: page < 1 → 1, pageSize 1..100.
// Üst sınır, istemcinin tek istekte tüm tabloyu çekip sunucuyu yormasını engeller.

namespace AjandaAI.Application.Common;

public class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = Math.Max(1, value);
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    public int Skip => (Page - 1) * PageSize;
}
