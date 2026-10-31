using mcpx_template_project.Domain.Entities.Business.CommSetting;
using mcpx_template_project.Domain.VO;
using CommSettingEntity = mcpx_template_project.Domain.Entities.Business.CommSetting.CommSetting;

namespace mcpx_template_project.Service.Business.CommSetting
{
    /// <summary>
    /// 通讯设定业务服务
    /// </summary>
    public interface ICommSettingService
    {
        /// <summary>
        /// 查询全部通讯设定（含单身明细，可按名称/IP 关键字过滤）
        /// </summary>
        Task<List<CommSettingVO>> QueryAllAsync(string? keyword = null);

        /// <summary>
        /// 根据ID查询通讯设定（含单身明细）
        /// </summary>
        Task<CommSettingVO?> GetByIdAsync(long id);

        /// <summary>
        /// 新增通讯设定（仅单头）
        /// </summary>
        Task<CommSettingVO> InsertAsync(CommSettingEntity setting);

        /// <summary>
        /// 修改通讯设定（仅单头，不动单身明细）
        /// </summary>
        Task<CommSettingVO> UpdateAsync(CommSettingEntity setting);

        /// <summary>
        /// 保存通讯设定的单身明细（区域配置，先删后插）
        /// </summary>
        Task SaveAreasAsync(long settingId, List<CommSettingArea> areas);

        /// <summary>
        /// 删除通讯设定
        /// </summary>
        Task DeleteAsync(long id);
    }
}
