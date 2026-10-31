using mcpx_template_project.Domain.Entities.Business.Trigger;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// Stream 5（报警处理）业务服务。
    /// 报警数据源：plc_trigger 表中 trigger_type=alarm 且配置了 ALID 的记录（S5F1 上报源）。
    /// </summary>
    public interface IS5AlarmService
    {
        /// <summary>
        /// S5F5/S5F7 查询：返回报警列表（ALCD/ALID/ALTX）。
        /// </summary>
        /// <param name="instanceId">SECS 实例 ID</param>
        /// <param name="alids">ALID 列表；空列表表示查询全部。</param>
        /// <param name="enabledOnly">true 只返回启用监控的报警（S5F7）；false 返回全部（S5F5）。</param>
        Task<List<AlarmInfo>> ListAlarmsAsync(long instanceId, IReadOnlyList<int> alids, bool enabledOnly);

        /// <summary>
        /// S5F3 使能/禁用报警上报。返回 ACKC5（0=OK，非 0=NG）。
        /// </summary>
        /// <param name="instanceId">实例 ID</param>
        /// <param name="enable">true 使能，false 禁用</param>
        /// <param name="alids">ALID 列表；空列表表示全部报警。</param>
        Task<byte> SetAlarmEnabledAsync(long instanceId, bool enable, IReadOnlyList<int> alids);
    }

    /// <summary>报警信息（S5F6/S5F8 报文元素：L,3 [ALCD, ALID, ALTX]）。</summary>
    public readonly struct AlarmInfo
    {
        /// <summary>报警码字节：bit8=发生/清除，bit7-1=类别</summary>
        public byte Alcd { get; init; }

        /// <summary>报警 ID</summary>
        public int Alid { get; init; }

        /// <summary>报警文本</summary>
        public string Altext { get; init; }
    }
}
