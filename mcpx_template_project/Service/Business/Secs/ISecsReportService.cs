using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using SecsReportEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsReport;
using SecsVidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsVid;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// SECS/GEM Report 业务服务：secs_report 表 + secs_report_vid 子表的增删改查。
    /// 一个 Report 关联一组 VID（SVID/DVID），通过子表 secs_report_vid 维护成员与上报顺序（sort）。
    /// </summary>
    public interface ISecsReportService
    {
        /// <summary>查询所有 Report（按实例过滤，含其 VID 成员明细，按 sort 排序）</summary>
        Task<List<SecsReportEntity>> QueryAllAsync(long instanceId);

        /// <summary>分页查询 Report（按实例过滤，含其 VID 成员明细，按 sort 排序）</summary>
        Task<PagedResult<SecsReportEntity>> QueryPagedAsync(long instanceId, PagingParameters paging);

        /// <summary>按 ID 查询单个 Report（含其 VID 成员明细）</summary>
        Task<SecsReportEntity?> GetByIdAsync(long id);

        /// <summary>新增 Report（含其 VID 成员明细，sort 按传入顺序编号）</summary>
        Task<SecsReportEntity> InsertAsync(SecsReportEntity report, IReadOnlyList<long> vidIds);

        /// <summary>修改 Report（覆盖其 VID 成员明细）</summary>
        Task<SecsReportEntity> UpdateAsync(SecsReportEntity report, IReadOnlyList<long> vidIds);

        /// <summary>切换启用状态（0=启用，1=停用），仅改 status 字段，不影响成员 VID</summary>
        Task UpdateStatusAsync(long id, string status);

        /// <summary>删除 Report（级联删除其 VID 成员子表）</summary>
        Task DeleteAsync(long id);

        /// <summary>查询指定实例下所有启用的 VID（供 Report 成员选择）</summary>
        Task<List<SecsVidEntity>> QueryVidsAsync(long instanceId);
    }
}
