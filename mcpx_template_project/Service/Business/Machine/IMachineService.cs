using MachineEntity = mcpx_template_project.Domain.Entities.Business.Machine.Machine;

namespace mcpx_template_project.Service.Business.Machine
{
    /// <summary>
    /// 机台业务服务：machine 表的增删改查。
    /// 机台是首页监控的一级概念，一台机台关联一条 PLC 通讯设定（comm_setting），
    /// 其运行时状态点位由 <c>MachinePlc</c>（纯内存 PLC 模型）承载，不落库。
    /// </summary>
    public interface IMachineService
    {
        /// <summary>查询所有机台（按 ID 排序）</summary>
        Task<List<MachineEntity>> QueryAllAsync();

        /// <summary>按 ID 查询单个机台</summary>
        Task<MachineEntity?> GetByIdAsync(long id);

        /// <summary>新增机台</summary>
        Task<MachineEntity> InsertAsync(MachineEntity machine);

        /// <summary>修改机台</summary>
        Task<MachineEntity> UpdateAsync(MachineEntity machine);

        /// <summary>删除机台</summary>
        Task DeleteAsync(long id);
    }
}
