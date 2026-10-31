using Secs4Net;
using mcpx_template_project.Domain.Entities.Business.Secs;
using mcpx_template_project.Domain.Enums;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// Stream 1（设备状态）业务服务：SVID 名值、选定状态数据、通信建立与在线/离线切换。
    /// 数据来源：secs_vid 表（vid_type=SVID）+ ISecsValueReader 取值 + ISecsStateService 状态机。
    /// </summary>
    public interface IS1StatusService
    {
        /// <summary>本机型号名（MDLN，文档约定 "VMIOSV"）</summary>
        string ModelName { get; }

        /// <summary>软件版本号（SOFTREV，文档约定 "v2.6.0"）</summary>
        string SoftwareRevision { get; }

        /// <summary>
        /// S1F1/S1F3 查询：返回选定 SVID 的当前值列表。
        /// </summary>
        /// <param name="instanceId">SECS 实例 ID</param>
        /// <param name="svids">要查询的 SVID 列表；空列表表示查询全部。</param>
        /// <returns>按请求顺序返回的值列表；某 SVID 不存在时对应项为 null（S1F4 约定空列表项）。</returns>
        Task<List<SecsValueResult>> GetStatusValuesAsync(long instanceId, IReadOnlyList<int> svids);

        /// <summary>
        /// S1F11 查询：返回选定 SVID 的名称与单位（S1F12）。
        /// </summary>
        /// <param name="instanceId">SECS 实例 ID</param>
        /// <param name="svids">要查询的 SVID 列表；空列表表示查询全部。</param>
        /// <returns>SVID → (名称, 单位)；不存在时名称/单位均为 null。</returns>
        Task<List<SvidNameUnit>> GetStatusNamesAsync(long instanceId, IReadOnlyList<int> svids);

        /// <summary>设备是否在线（Control State ∈ {OnlineLocal, OnlineRemote}）。</summary>
        bool IsOnline(long instanceId);

        /// <summary>获取设备控制状态（供 S1F4 的 ControlStatus 之类回报）。</summary>
        SecsControlState GetControlState(long instanceId);
    }

    /// <summary>SVID 名称与单位（S1F12 报文元素）。</summary>
    public readonly struct SvidNameUnit
    {
        public int Svid { get; init; }
        public string? Name { get; init; }
        public string? Units { get; init; }
    }
}
