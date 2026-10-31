using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using SecsVidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsVid;
using SecsVidType = mcpx_template_project.Domain.Entities.Business.Secs.SecsVidType;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// SECS/GEM VID 业务服务：secs_vid 表的增删改查。
    /// VID 统一承载 SVID/DVID/ECID 三类（vid_type 区分），值来源 source 区分 db/plc。
    /// </summary>
    public interface ISecsVidService
    {
        /// <summary>查询所有 VID（按实例过滤，可按类型过滤，可按名称模糊匹配）</summary>
        Task<List<SecsVidEntity>> QueryAllAsync(long instanceId, SecsVidType? vidType = null, string? keyword = null);

        /// <summary>分页查询 VID（按实例 + 类型/名称/设备代号/点位地址/状态过滤）</summary>
        Task<PagedResult<SecsVidEntity>> QueryPagedAsync(
            long instanceId,
            SecsVidType? vidType,
            string? keyword,
            string? deviceCode,
            string? plcAddress,
            string? status,
            PagingParameters paging);

        /// <summary>按 ID 查询单个 VID</summary>
        Task<SecsVidEntity?> GetByIdAsync(long id);

        /// <summary>新增 VID</summary>
        Task<SecsVidEntity> InsertAsync(SecsVidEntity vid);

        /// <summary>修改 VID</summary>
        Task<SecsVidEntity> UpdateAsync(SecsVidEntity vid);

        /// <summary>切换启用状态（0=启用，1=停用），仅改 status 字段</summary>
        Task UpdateStatusAsync(long id, string status);

        /// <summary>删除 VID</summary>
        Task DeleteAsync(long id);
    }
}
