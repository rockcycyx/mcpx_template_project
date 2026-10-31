namespace mcpx_template_project.Domain.Enums
{
    /// <summary>
    /// 机台运行状态枚举（对应 PLC D3114 状态位）。
    /// </summary>
    public enum MachineState
    {
        /// <summary>停機</summary>
        Stop = 0,

        /// <summary>待機</summary>
        Idle = 1,

        /// <summary>自動運轉</summary>
        Running = 2
    }
}
