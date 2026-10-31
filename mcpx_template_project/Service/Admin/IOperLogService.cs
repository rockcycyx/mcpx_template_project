using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Service.Admin
{
    public interface IOperLogService
    {
        /// <summary>
        /// 异步保存操作日志
        /// </summary>
        Task SaveOperLogAsync(SysOperLog operLog);

        /// <summary>
        /// 分页查询操作日志
        /// </summary>
        Task<PagedResult<SysOperLogVO>> QueryOperLogsPagedAsync(SysOperLogQuery query, PagingParameters paging);

        /// <summary>
        /// 根据ID查询操作日志
        /// </summary>
        Task<SysOperLogVO?> GetOperLogByIdAsync(long operId);

        /// <summary>
        /// 删除操作日志
        /// </summary>
        Task DeleteOperLogAsync(long operId);

        /// <summary>
        /// 清空操作日志
        /// </summary>
        Task ClearOperLogsAsync();
    }
}
