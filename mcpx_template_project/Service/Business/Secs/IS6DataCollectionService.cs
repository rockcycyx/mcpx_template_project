namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// Stream 6（数据采集）业务服务。
    /// 覆盖：事件报表（CEID → 关联 Report → VID 值）、个体报表（RPTID → VID 值）、请求 Spooled 数据。
    /// 数据来源：secs_ceid / secs_report / secs_report_vid / secs_vid 表 + ISecsValueReader。
    /// </summary>
    public interface IS6DataCollectionService
    {
        /// <summary>
        /// S6F15 事件报表请求：返回指定 CEID 关联的报表数据（S6F16）。
        /// </summary>
        /// <param name="instanceId">实例 ID</param>
        /// <param name="ceid">CEID</param>
        /// <returns>事件报表数据；CEID 不存在或未关联报表时为空。</returns>
        Task<EventReportData?> GetEventReportAsync(long instanceId, int ceid);

        /// <summary>
        /// S6F19 个体报表请求：返回指定 RPTID 的 VID 值列表（S6F20）。
        /// </summary>
        /// <param name="instanceId">实例 ID</param>
        /// <param name="rptid">RPTID</param>
        /// <returns>VID 值列表；RPTID 不存在时为空。</returns>
        Task<List<SecsValueResult>> GetIndividualReportAsync(long instanceId, int rptid);

        /// <summary>
        /// S6F23 请求 Spooled 数据：返回 RSDA 应答码。
        /// 本项目 spooling 未落地，统一回「无 spooled 数据」。
        /// </summary>
        /// <param name="instanceId">实例 ID</param>
        /// <param name="transmit">true=发送，false=清除</param>
        Task<byte> RequestSpooledDataAsync(long instanceId, bool transmit);
    }

    /// <summary>事件报表数据（S6F16 报文主体：DATAID + CEID + 报表列表）。</summary>
    public sealed class EventReportData
    {
        /// <summary>DATAID（恒 0）</summary>
        public uint DataId { get; set; }

        /// <summary>CEID</summary>
        public int Ceid { get; set; }

        /// <summary>关联的报表列表：RPTID → 该报表的 VID 值列表（按上报顺序）。</summary>
        public List<(int Rptid, List<SecsValueResult> Values)> Reports { get; } = new();
    }
}
