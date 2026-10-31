namespace mcpx_template_project.Domain.VO.Trigger
{
    /// <summary>
    /// PLC 事件处理器选项（供配点界面下拉框展示）。
    /// </summary>
    public class PlcEventHandlerOption
    {
        /// <summary>处理器 Key（存表 action_key）</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>处理器显示名</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>处理器描述</summary>
        public string? Description { get; set; }

        /// <summary>处理器实现类型（运行时反射用，不参与序列化）</summary>
        public Type? HandlerType { get; set; }

        /// <summary>下拉框显示文本</summary>
        public override string ToString() => string.IsNullOrEmpty(Description)
            ? DisplayName
            : $"{DisplayName}（{Description}）";
    }
}
