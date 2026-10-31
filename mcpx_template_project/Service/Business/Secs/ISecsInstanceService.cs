using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using SecsInstanceEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsInstance;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// SECS/GEM 实例业务服务：secs_instance 表的增删改查。
    /// 实例是 SECS 会话的一级概念，承载通讯参数（IP/Port/DeviceId/T3~T8/ConnectMode），
    /// 并可关联一台 PLC 通讯设定（comm_setting）。
    /// </summary>
    public interface ISecsInstanceService
    {
        /// <summary>查询所有实例（可按名称关键字过滤）</summary>
        Task<List<SecsInstanceEntity>> QueryAllAsync(string? keyword = null);

        /// <summary>分页查询实例</summary>
        Task<PagedResult<SecsInstanceEntity>> QueryPagedAsync(PagingParameters paging, string? keyword = null);

        /// <summary>按 ID 查询单个实例</summary>
        Task<SecsInstanceEntity?> GetByIdAsync(long id);

        /// <summary>新增实例</summary>
        Task<SecsInstanceEntity> InsertAsync(SecsInstanceEntity instance);

        /// <summary>修改实例</summary>
        Task<SecsInstanceEntity> UpdateAsync(SecsInstanceEntity instance);

        /// <summary>删除实例</summary>
        Task DeleteAsync(long id);

        /// <summary>查询所有启用的实例（供通讯层自动启动）</summary>
        Task<List<SecsInstanceEntity>> QueryEnabledAsync();
    }
}
