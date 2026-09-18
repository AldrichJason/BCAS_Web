namespace BCAS.Application.Common;

/// <summary>Normalised paging/search parameters shared by the listing endpoints.</summary>
public class PageQuery
{
    private const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = 10;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 10,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    public string? Search { get; set; }

    public int Offset => (Page - 1) * PageSize;
}
