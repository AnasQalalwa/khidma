namespace Khidma.Api.Contracts.Common;

public sealed class PageQuery
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 50;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;

    public string? Status { get; set; }

    public (int Page, int PageSize) Normalize()
    {
        var page = Page < 1 ? 1 : Page;
        var pageSize = PageSize < 1
            ? DefaultPageSize
            : Math.Min(PageSize, MaxPageSize);
        return (page, pageSize);
    }
}
