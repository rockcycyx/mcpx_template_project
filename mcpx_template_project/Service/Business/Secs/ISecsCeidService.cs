using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using SecsCeidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsCeid;
using SecsReportEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsReport;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// SECS/GEM 收集事件（CEID/Event）业务服务：secs_ceid 表 + secs_ceid_report 子表的增删改查。
    /// 一个 CEID 关联一组 Report，通过子表 secs_ceid_report 维护成员与上报顺序（sort）。
    /// </summary>
    public interface ISecsCeidService
    {
        /// <summary>查询所有 CEID（按实例过滤，含其 Report 成员明细，按 sort 排序）</summary>
        Task<List<SecsCeidEntity>> QueryAllAsync(long instanceId);

        /// <summary>分页查询 CEID（按实例过滤，含其 Report 成员明细，按 sort 排序）</summary>
        Task<PagedResult<SecsCeidEntity>> QueryPagedAsync(long instanceId, PagingParameters paging);

        /// <summary>按 ID 查询单个 CEID（含其 Report 成员明细）</summary>
        Task<SecsCeidEntity?> GetByIdAsync(long id);

        /// <summary>新增 CEID（含其 Report 成员明细，sort 按传入顺序编号）</summary>
        Task<SecsCeidEntity> InsertAsync(SecsCeidEntity ceid, IReadOnlyList<long> reportIds);

        /// <summary>修改 CEID（覆盖其 Report 成员明细）</summary>
        Task<SecsCeidEntity> UpdateAsync(SecsCeidEntity ceid, IReadOnlyList<long> reportIds);

        /// <summary>切换启用状态（0=启用，1=停用），仅改 status 字段，不影响成员 Report</summary>
        Task UpdateStatusAsync(long id, string status);

        /// <summary>删除 CEID（级联删除其 Report 成员子表）</summary>
        Task DeleteAsync(long id);

        /// <summary>查询指定实例下所有启用的 Report（供 CEID 成员选择）</summary>
        Task<List<SecsReportEntity>> QueryReportsAsync(long instanceId);
    }
}
