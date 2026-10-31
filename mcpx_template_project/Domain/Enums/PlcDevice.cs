namespace mcpx_template_project.Domain.Enums
{
    /// <summary>
    /// PLC 设备代号（软元件前缀）。
    /// 用于注解声明读写点位时替代魔法字符串，避免写错 "D"/"M" 这类代号。
    /// 枚举名即三菱软元件前缀，底层通讯层再转回字符串。
    /// </summary>
    public enum PlcDevice
    {
        /// <summary>数据寄存器（字）</summary>
        D = 0,

        /// <summary>链接寄存器（字）</summary>
        W = 1,

        /// <summary>内部继电器（位）</summary>
        M = 2,

        /// <summary>输入继电器（位）</summary>
        X = 3,

        /// <summary>输出继电器（位）</summary>
        Y = 4,

        /// <summary>锁存继电器（位）</summary>
        L = 5,

        /// <summary>报警器（位）</summary>
        F = 6,

        /// <summary>边沿继电器（位）</summary>
        V = 7,

        /// <summary>链接继电器（位）</summary>
        B = 8,

        /// <summary>步进继电器（位）</summary>
        S = 9,

        /// <summary>特殊继电器（位）</summary>
        SM = 10,

        /// <summary>特殊寄存器（字）</summary>
        SD = 11
    }
}
