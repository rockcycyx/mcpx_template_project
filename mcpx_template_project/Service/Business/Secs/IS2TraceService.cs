using Secs4Net;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// Stream 2 Trace（采样数据）业务服务。
    /// 职责：维护各实例的 Trace 定义（S2F23 初始化），按采样周期（DSPER）后台采集 SVID 值，
    /// 并主动向 Host 上报 S6F1（Trace Data Send）。
    /// Trace 定义为内存态（与 <see cref="ISecsStateService"/> 一致），进程重启即清空。
    /// </summary>
    public interface IS2TraceService
    {
        /// <summary>
        /// S2F23 初始化/禁用一个 Trace。
        /// TOTSMP=0 表示禁用（删除已存在的 Trace）；否则注册或覆盖该 TRID 的采样定义。
        /// </summary>
        /// <param name="instanceId">SECS 实例 ID</param>
        /// <param name="traceId">TRID</param>
        /// <param name="dsper">单个样本采样周期文本（hhmmss / hhmmsscc / hhmmsszzz）</param>
        /// <param name="totSmp">总采样数（0=禁用；SMPLN 从 1 递增到 TOTSMP 后自动停用）</param>
        /// <param name="repgsz">每包 S6F1 上报的样本数（REPGSZ）</param>
        /// <param name="svids">SVID 列表</param>
        /// <returns>TIAACK 应答码</returns>
        Task<byte> InitializeAsync(long instanceId, uint traceId, string dsper, uint totSmp, uint repgsz, IReadOnlyList<int> svids);

        /// <summary>移除指定实例的全部 Trace（连接断开/停止监听时清理）。</summary>
        void Clear(long instanceId);
    }
}
