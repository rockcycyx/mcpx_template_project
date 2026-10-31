namespace mcpx_template_project.Domain.Enums
{
    /// <summary>
    /// 蜂鸣器状态（PLC 写点 D3013 / 读点 D3116）。
    /// </summary>
    public enum BuzzerState
    {
        /// <summary>无蜂鸣</summary>
        Off = 0,

        /// <summary>蜂鸣</summary>
        On = 1
    }
}
