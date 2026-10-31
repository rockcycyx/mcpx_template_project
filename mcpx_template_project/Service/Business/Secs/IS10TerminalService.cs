namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// Stream 10（终端服务）业务服务。
    /// 接收 Host 下发的终端显示消息（S10F3），回执显示结果（ACKC10）。
    /// </summary>
    public interface IS10TerminalService
    {
        /// <summary>
        /// S10F3 终端显示：接收并「显示」文本，返回 ACKC10。
        /// 本实现无物理终端，仅记录日志并返回「已接受显示」。
        /// </summary>
        /// <param name="instanceId">实例 ID</param>
        /// <param name="tid">文本消息类型（0=广播，1=警告）</param>
        /// <param name="text">显示文本</param>
        /// <returns>ACKC10 应答码</returns>
        Task<byte> DisplayAsync(long instanceId, byte tid, string text);
    }
}
