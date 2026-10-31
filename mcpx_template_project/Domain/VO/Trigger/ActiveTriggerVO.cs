namespace mcpx_template_project.Domain.VO.Trigger
{
    /// <summary>
    /// 正在发生的触发器视图对象（供警报列表页展示）。
    /// 同时承载警报（trigger_type=alarm）与事件（trigger_type=event）的活跃状态。
    /// </summary>
    public class ActiveTriggerVO
    {
        /// <summary>触发器 ID</summary>
        public long TriggerId { get; set; }

        /// <summary>触发名 / 警报名</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>触发器类型（alarm / event）</summary>
        public string TriggerType { get; set; } = "alarm";

        /// <summary>备注</summary>
        public string? Remark { get; set; }

        /// <summary>所属通讯设定 ID</summary>
        public long CommSettingId { get; set; }

        /// <summary>完整点位（设备代号 + 地址）</summary>
        public string FullAddress { get; set; } = string.Empty;

        /// <summary>当前值</summary>
        public string CurrentValue { get; set; } = string.Empty;
    }
}
