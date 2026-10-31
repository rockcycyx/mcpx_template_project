using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mcpx_template_project.Domain.Common
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; init; } = Array.Empty<T>();
        public int TotalCount { get; init; }
        public int PageIndex { get; init; } // 1-based
        public int PageSize { get; init; }

        // TODO
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
