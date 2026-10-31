using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Service.Business.Plc
{
    /// <summary>
    /// PLC 自动连线服务：负责在应用启动（登录后）时，
    /// 扫描所有「启动连线模式 = auto」的通讯设定并自动建立通讯连接。
    /// 连线状态为纯内存态（由 <see cref="IPlcCommunicationService"/> 维护），不写入数据库。
    /// </summary>
    public interface IPlcAutoConnectService
    {
        /// <summary>
        /// 自动连接所有 ConnectMode=auto 的通讯设定。
        /// 逐条尝试连接，单条失败不影响其它设定。
        /// </summary>
        /// <returns>成功连接的通讯设定数量</returns>
        Task<int> AutoConnectAllAsync();
    }
}
