using AuthServer.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Infrastructure.Persistence.Ef;

public static class QueryableExtensions
{
    // Pages an ordered query in the database and maps the page to domain objects.
    // The page size is clamped to 1..MaxPageSize and the page number to the existing pages.
    public static async Task<PagedList<TDomain>> ToPagedListAsync<TEntity, TDomain>(
        this IQueryable<TEntity> orderedQuery, PagedListQuery query, Func<TEntity, TDomain> toDomain)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, PagedListQuery.MaxPageSize);

        var totalCount = await orderedQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var pageNumber = Math.Clamp(query.PageNumber, 1, totalPages);

        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<TDomain>(items.Select(toDomain).ToList(), pageNumber, pageSize, totalCount);
    }

    public static string ToContainsPattern(this string searchTerm) =>
        $"%{searchTerm.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
}
