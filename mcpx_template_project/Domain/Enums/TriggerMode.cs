namespace mcpx_template_project.Domain.Enums
{
    /// <summary>
    /// PLC 触发器触发方式。
    /// </summary>
    public enum TriggerMode
    {
        /// <summary>上升沿触发：点位由 OFF→ON 时触发一次（持续 ON 不重复触发）</summary>
        On = 0,

        /// <summary>值变化触发：点位值发生变化即触发（每次变化沿都触发）</summary>
        Change = 1
    }
}
