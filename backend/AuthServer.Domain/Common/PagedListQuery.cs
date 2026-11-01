namespace AuthServer.Domain.Common;

public class PagedListQuery
{
    public const int MaxPageSize = 100;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
}
