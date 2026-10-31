using mcpx_template_project.Domain.Model;
using PlcTriggerEntity = mcpx_template_project.Domain.Entities.Business.Trigger.PlcTrigger;

namespace mcpx_template_project.Service.Business.Trigger
{
    /// <summary>
    /// PLC 触发器业务服务：触发器配置（警报 + 事件）的增删改查 + Excel 导入导出。
    /// </summary>
    public interface IPlcTriggerService
    {
        /// <summary>查询所有触发器（按查询条件多字段筛选）</summary>
        Task<List<PlcTriggerEntity>> QueryAllAsync(PlcTriggerQuery? query = null);

        /// <summary>查询指定通讯设定下的所有触发器</summary>
        Task<List<PlcTriggerEntity>> QueryBySettingAsync(long commSettingId);

        /// <summary>按 ID 查询单个触发器</summary>
        Task<PlcTriggerEntity?> GetByIdAsync(long id);

        /// <summary>新增触发器</summary>
        Task<PlcTriggerEntity> InsertAsync(PlcTriggerEntity trigger);

        /// <summary>修改触发器</summary>
        Task<PlcTriggerEntity> UpdateAsync(PlcTriggerEntity trigger);

        /// <summary>删除触发器</summary>
        Task DeleteAsync(long id);

        /// <summary>切换触发器启用状态（status 1↔0）</summary>
        Task SwitchStatusAsync(long id, string status);

        /// <summary>更新警报类别（alarm_category，仅 alarm 类型有效）</summary>
        Task UpdateCategoryAsync(long id, int category);

        /// <summary>
        /// 批量导入（Excel）：同一「comm_setting_id + device_code + address」即覆盖，否则新增。
        /// </summary>
        /// <returns>返回 (新增数, 覆盖数)</returns>
        Task<(int inserted, int updated)> ImportAsync(List<PlcTriggerEntity> triggers);

        /// <summary>导出全部触发器到 Excel</summary>
        void Export(string filePath, IEnumerable<PlcTriggerEntity>? data = null);

        /// <summary>导出模板 Excel（仅表头，供用户填写）</summary>
        void ExportTemplate(string filePath);

        /// <summary>从 Excel 文件解析触发器列表（供导入使用，不落库）</summary>
        List<PlcTriggerEntity> ImportFromExcel(string filePath);
    }
}
