using mcpx_template_project.Core.Handler;

namespace mcpx_template_project.Service.Business.Trigger
{
    /// <summary>
    /// PLC 事件触发上下文：传递给 <see cref="IPlcEventHandler.HandleAsync"/> 的运行时信息。
    /// </summary>
    public class PlcEventContext
    {
        /// <summary>触发器 ID</summary>
        public long TriggerId { get; set; }

        /// <summary>所属通讯设定 ID</summary>
        public long CommSettingId { get; set; }

        /// <summary>设备代号（D/W/M/X/Y ...）</summary>
        public string DeviceCode { get; set; } = string.Empty;

        /// <summary>点位地址</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>完整点位（设备代号 + 地址）</summary>
        public string FullAddress => $"{DeviceCode}{Address}";

        /// <summary>触发名 / 警报名</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>当前值（位设备为 ON/OFF，字设备为数值）</summary>
        public string CurrentValue { get; set; } = string.Empty;

        /// <summary>上一次值（触发前）</summary>
        public string? PreviousValue { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }

        /// <summary>触发时间</summary>
        public DateTime TriggerTime { get; set; } = DateTime.Now;
    }
}
