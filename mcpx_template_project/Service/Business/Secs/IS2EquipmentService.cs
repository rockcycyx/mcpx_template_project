using Secs4Net;
using mcpx_template_project.Domain.Entities.Business.Secs;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// Stream 2（设备控制与诊断）业务服务。
    /// 覆盖：设备常量（ECID）、时间同步、Trace 初始化、定义/关联/使能报表、远程命令、
    /// Reset Spooling、变量限值属性（Limit Monitoring）、Loopback。
    /// 数据来源：secs_vid（vid_type=DVID/ECID）+ ISecsValueReader + ISecsStateService。
    /// </summary>
    public interface IS2EquipmentService
    {
        /// <summary>当前时间（S2F18 DTD / S2F31 DTS），格式 YYYYMMDDHHMMSS。</summary>
        string CurrentTimeText { get; }

        /// <summary>
        /// S2F13/S2F29 查询：返回设备常量（ECID）的值。
        /// </summary>
        /// <param name="instanceId">实例 ID</param>
        /// <param name="ecids">ECID 列表；空列表表示查询全部。</param>
        Task<List<SecsValueResult>> GetConstantValuesAsync(long instanceId, IReadOnlyList<int> ecids);

        /// <summary>
        /// S2F29 查询：返回设备常量（ECID）的名称/最小/最大/默认/单位（S2F30）。
        /// </summary>
        Task<List<ConstantNameUnit>> GetConstantNamesAsync(long instanceId, IReadOnlyList<int> ecids);

        /// <summary>
        /// S2F15 写入：更新设备常量值。返回是否成功与错误码。
        /// 当前实现仅支持 source=db 的 ECID（更新 default_value）；plc 来源暂不支持写回。
        /// </summary>
        /// <param name="instanceId">实例 ID</param>
        /// <param name="constants">ECID → 新值文本 的字典</param>
        /// <returns>0=成功，非 0=EAC 错误码</returns>
        Task<byte> UpdateConstantsAsync(long instanceId, IReadOnlyDictionary<int, string> constants);

        /// <summary>
        /// S2F25 Loopback：原样返回二进制串（S2F26）。
        /// </summary>
        byte[] Echo(byte[] data);

        /// <summary>
        /// S2F23 Trace 初始化：定义/禁用 Trace。
        /// 返回 TIAACK 应答码（0=OK，1=SVID 过多，2=Trace 数量超限，3=周期非法）。
        /// DSPER=单个样本采样间隔；S6F1 上报间隔=DSPER×REPGSZ；总采样时长=DSPER×TOTSMP。
        /// </summary>
        /// <param name="instanceId">实例 ID</param>
        /// <param name="traceId">TRID</param>
        /// <param name="dsper">采样周期文本（hhmmss 系列）</param>
        /// <param name="totSmp">总采样数（0=禁用）</param>
        /// <param name="repgsz">每包 S6F1 样本数</param>
        /// <param name="svids">SVID 列表</param>
        Task<byte> InitializeTraceAsync(long instanceId, uint traceId, string dsper, uint totSmp, uint repgsz, IReadOnlyList<int> svids);

        /// <summary>
        /// S2F41 远程命令：执行 REMOTE/LOCAL/ABORT/PP_SELECT 等命令。
        /// 返回 (HCACK, 无效参数列表)。参数列表为 null 表示无错误。
        /// </summary>
        /// <param name="instanceId">实例 ID</param>
        /// <param name="rcmd">命令串</param>
        /// <param name="parameters">参数名 → 参数值文本 字典（保持顺序）</param>
        Task<(byte Hcack, IReadOnlyList<(string Name, byte Ack)>? Errors)> ExecuteRemoteCommandAsync(
            long instanceId, string rcmd, IReadOnlyList<(string Name, string Value)> parameters);

        /// <summary>
        /// S2F49 增强远程命令：执行带参数组的远程命令。
        /// 返回 (HCACK, 无效参数列表)。
        /// </summary>
        Task<(byte Hcack, IReadOnlyList<(string Name, byte Ack)>? Errors)> ExecuteEnhancedCommandAsync(
            long instanceId, string objSpec, string rcmd, IReadOnlyList<(string Name, string Value)> parameters);

        /// <summary>
        /// S2F43 Reset Spooling：重置 spool 配置。返回 (RSPACK, 错误流列表)。
        /// 本项目 spooling 未落地，直接回接受。
        /// </summary>
        Task<(byte Rspack, IReadOnlyList<(byte Stream, IReadOnlyList<byte> Functions)>? Errors)> ResetSpoolingAsync(
            long instanceId, IReadOnlyList<(byte Stream, IReadOnlyList<byte> Functions)> config);

        /// <summary>
        /// S2F45 定义变量限值属性：返回 VLAACK 应答码。
        /// 本项目 Limit Monitoring 仅校验不落地，统一回接受。
        /// </summary>
        Task<byte> DefineLimitAttributesAsync(long instanceId);

        /// <summary>定义报表（S2F33）：校验 RPTID/VID 是否合法，返回 DRACK。</summary>
        Task<byte> DefineReportAsync(long instanceId, IReadOnlyList<(int Rptid, IReadOnlyList<int> Vids)> reports);

        /// <summary>关联事件报表（S2F35）：校验 CEID/RPTID，返回 LRACK。</summary>
        Task<byte> LinkEventReportAsync(long instanceId, IReadOnlyList<(int Ceid, IReadOnlyList<int> Rptids)> links);

        /// <summary>使能/禁用事件报表（S2F37）：校验 CEID，返回 ERACK。</summary>
        Task<byte> EnableDisableEventAsync(long instanceId, bool enable, IReadOnlyList<int> ceids);

        /// <summary>S2F31 时间同步：设置设备时间，返回 TIACK。</summary>
        Task<byte> SetTimeAsync(long instanceId, string timeText);
    }

    /// <summary>设备常量名称/范围信息（S2F30 报文元素）。</summary>
    public readonly struct ConstantNameUnit
    {
        public int Ecid { get; init; }
        public string? Name { get; init; }
        public string? Min { get; init; }
        public string? Max { get; init; }
        public string? Default { get; init; }
        public string? Units { get; init; }
    }
}
