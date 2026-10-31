using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mcpx_template_project.Domain.Model
{
    public class PagingParameters
    {
        // 1-based page index
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // 排序/过滤/搜索的占位字段
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
        public string? Filter { get; set; }
    }
}
