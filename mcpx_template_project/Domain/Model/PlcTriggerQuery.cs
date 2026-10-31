namespace mcpx_template_project.Domain.Model
{
    /// <summary>
    /// 触发器查询条件（列表页多条件筛选）。
    /// 各字段为 null 或空表示不参与过滤。
    /// </summary>
    public class PlcTriggerQuery
    {
        /// <summary>触发名/警报名（模糊匹配）</summary>
        public string? Keyword { get; set; }

        /// <summary>通讯设定ID（精确匹配）</summary>
        public long? CommSettingId { get; set; }

        /// <summary>设备代号（精确匹配，如 M/X/Y）</summary>
        public string? DeviceCode { get; set; }

        /// <summary>点位地址（模糊匹配）</summary>
        public string? Address { get; set; }

        /// <summary>触发器类型（alarm / event，精确匹配）</summary>
        public string? TriggerType { get; set; }

        /// <summary>触发方式（on / change，精确匹配）</summary>
        public string? TriggerMode { get; set; }

        /// <summary>事件处理器 Key（精确匹配）</summary>
        public string? ActionKey { get; set; }

        /// <summary>警报类别（0~8，精确匹配；null 不筛选）</summary>
        public int? AlarmCategory { get; set; }
    }
}
