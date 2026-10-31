using Secs4Net;
using SecsVidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsVid;
using SecsDataType = mcpx_template_project.Domain.Entities.Business.Secs.SecsDataType;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// SECS VID 值解析结果：一个 VID 在当前时刻的值（已转成 Secs4Net Item 或标记为不存在）。
    /// 各 Stream Service（S1/S2/S6）在处理 SVID/DVID/ECID 查询时，用本结构组装回复报文。
    /// </summary>
    public readonly struct SecsValueResult
    {
        /// <summary>VID 编号（SVID/DVID/ECID）</summary>
        public int VidId { get; init; }

        /// <summary>值是否存在（VID 未定义或停用时为 false）</summary>
        public bool Exists { get; init; }

        /// <summary>当前值（SecsItem）。Exists=false 时按报文约定可为空 List 或默认。</summary>
        public Item? Value { get; init; }
    }

    /// <summary>
    /// SECS VID 值读取门面：负责「db 固定值 / PLC 点位缓存」两种来源 → 当前值 Item 的转换。
    /// 它只管「取值」，不管「哪个 VID 该被哪些消息查询」（那由各 Stream Service 决定）。
    /// </summary>
    public interface ISecsValueReader
    {
        /// <summary>
        /// 读取单个 VID 的当前值。
        /// </summary>
        /// <param name="instanceId">SECS 实例 ID</param>
        /// <param name="commSettingId">实例关联的 PLC 通讯设定 ID（可为 null：db 来源或纯上报实例）</param>
        /// <param name="vid">VID 定义</param>
        /// <returns>值结果；VID 停用或类型不支持时为 Exists=false</returns>
        Task<SecsValueResult> ReadAsync(long instanceId, long? commSettingId, SecsVidEntity vid);

        /// <summary>
        /// 把文本值按 VID 声明的 data_type 转成 SecsItem（用于 db 固定值或 PLC 文本值）。
        /// 转换失败返回 null（调用方视为值不存在）。
        /// </summary>
        Item? Convert(SecsDataType dataType, string? textValue);
    }
}
