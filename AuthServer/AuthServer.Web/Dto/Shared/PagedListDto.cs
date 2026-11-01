using AuthServer.Web.Shared;

namespace AuthServer.Web.Dto.Shared;

public class PagedListDto<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public required int TotalPages { get; init; }
    public required bool HasPrevious { get; init; }
    public required bool HasNext { get; init; }
}

public static class PagedListDtoExtensions
{
    extension<T>(PagedList<T> source)
    {
        public PagedListDto<T> ToDto() => source.ToDto(x => x);

        public PagedListDto<TDto> ToDto<TDto>(Func<T, TDto> map) => new()
        {
            Items = source.Items.Select(map).ToList(),
            PageNumber = source.PageNumber,
            PageSize = source.PageSize,
            TotalCount = source.TotalCount,
            TotalPages = source.TotalPages,
            HasPrevious = source.HasPrevious,
            HasNext = source.HasNext,
        };
    }
}
