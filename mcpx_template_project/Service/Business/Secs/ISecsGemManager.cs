using mcpx_template_project.Domain.Model.Secs;
using Secs4Net;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// SECS/GEM 多设备管理器：一个 EQP 端可能服务多台设备，
    /// 每台设备对应一个独立的 SECS 通讯引擎实例（独立 IP/Port/DeviceId/连接/状态）。
    /// 本接口作为多设备门面，按 deviceKey 管理引擎的启停、路由与主动发送。
    /// </summary>
    public interface ISecsGemManager
    {
        /// <summary>
        /// 启动指定设备的监听。
        /// 若该设备已存在引擎则重启；不存在则新建。
        /// </summary>
        /// <param name="deviceKey">设备唯一标识（建议用 DeviceId 或业务编码）</param>
        /// <param name="config">该设备的通讯配置</param>
        Task<bool> StartAsync(string deviceKey, SecsGemConfiguration config);

        /// <summary>停止指定设备的监听并释放资源。</summary>
        void Stop(string deviceKey);

        /// <summary>停止所有设备的监听。</summary>
        void StopAll();

        /// <summary>
        /// 指定设备主动向 Host 发送消息（S6F1/S5F1/S9Fx 等上报）。
        /// 仅该设备处于 COMMUNICATING 时发送。
        /// </summary>
        Task<bool> SendAsync(string deviceKey, SecsMessage msg, CancellationToken ct);

        /// <summary>获取指定设备的引擎；不存在返回 null。</summary>
        ISecsGemCommunicationService? GetEngine(string deviceKey);

        /// <summary>当前所有已注册设备引擎（快照）。</summary>
        IReadOnlyDictionary<string, ISecsGemCommunicationService> Engines { get; }

        /// <summary>指定设备是否在运行。</summary>
        bool IsRunning(string deviceKey);

        /// <summary>指定设备是否已通信（COMMUNICATING）。</summary>
        bool IsCommunicating(string deviceKey);
    }
}
