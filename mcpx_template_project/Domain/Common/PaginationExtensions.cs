using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Domain.Model;

namespace mcpx_template_project.Domain.Common
{
    /// <summary>
    /// IQueryable 分页扩展，统一处理 Count + Skip/Take
    /// </summary>
    public static class PaginationExtensions
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> source, PagingParameters? paging)
        {
            var pageIndex = paging?.PageIndex >= 1 ? paging.PageIndex : 1;
            var pageSize = paging?.PageSize > 0 ? paging.PageSize : 10;

            var total = await source.CountAsync();

            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }
    }
}
