namespace mcpx_template_project.Domain.Enums
{
    /// <summary>
    /// 蓝灯状态（PLC 写点 D3012 / 读点 D3115）。
    /// </summary>
    public enum BlueLampState
    {
        /// <summary>燈灭</summary>
        Off = 0,

        /// <summary>燈常亮</summary>
        On = 1,

        /// <summary>燈閃爍</summary>
        Blink = 2
    }
}
