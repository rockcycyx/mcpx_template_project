using mcpx_template_project.Domain.Model.Secs;
using Secs4Net;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// SECS/GEM 通讯引擎服务：本项目作为 Equipment（Passive）端，
    /// 监听本机 IP/Port 等待 Host 主动连接。
    /// 连接状态为纯内存态，由引擎实时维护，不落库。
    /// </summary>
    public interface ISecsGemCommunicationService
    {
        /// <summary>
        /// 启动监听（Passive 端开始等待 Host 连接）。
        /// 监听中修改配置需先 Stop 再 Start。
        /// </summary>
        /// <param name="config">通讯配置（IP/Port/DeviceID 等）</param>
        /// <returns>true=启动成功；false=失败（详见 LastError）</returns>
        Task<bool> StartAsync(SecsGemConfiguration config);

        /// <summary>本引擎所属的 SECS 实例 ID（多实例场景下用于消息上下文注入；单实例场景为 null）。</summary>
        long? InstanceId { get; set; }

        /// <summary>停止监听并释放底层连接资源。</summary>
        void Stop();

        /// <summary>
        /// 设备端主动向 Host 发送一条消息（如 S6F1 事件、S5F1 警报、S9Fx 错误等）。
        /// 仅当已处于 COMMUNICATING 状态时发送，否则返回 false。
        /// </summary>
        /// <param name="msg">要发送的消息（replyExpected 由调用方决定）</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>true=已发送；false=未通信或发送失败</returns>
        Task<bool> SendAsync(SecsMessage msg, CancellationToken ct);

        /// <summary>是否处于监听/已连接状态。</summary>
        bool IsRunning { get; }

        /// <summary>是否已建立通信（COMMUNICATING 状态）。</summary>
        bool IsCommunicating { get; }

        /// <summary>最近一次启动失败的错误描述。</summary>
        string? LastError { get; }

        /// <summary>当前连接状态（Connecting/Connected/Selected/Retry/未启动）。</summary>
        string StateText { get; }

        /// <summary>对端（Host）IP 地址，未连接时为 "N/A"。</summary>
        string DeviceIpAddress { get; }

        /// <summary>连接状态变化时触发。</summary>
        event EventHandler? StateChanged;

        /// <summary>收到来自 Host 的主消息时触发（供后续业务扩展，如 S1F13 握手、事件上报等）。</summary>
        event EventHandler<string>? MessageReceived;
    }
}
