namespace mcpx_template_project.Domain.Entities.Business.Secs
{
    /// <summary>
    /// SECS/GEM 数据变量类型（VID 的 data_type）。
    /// 成员名即 SECS 标准格式码，配合 EF HasConversion&lt;string&gt; 以字符串落库，兼容旧数据。
    /// </summary>
    public enum SecsDataType
    {
        /// <summary>列表（List）</summary>
        L = 0,

        /// <summary>二进制（Binary）</summary>
        B = 1,

        /// <summary>布尔（Boolean）</summary>
        BOOLEAN = 2,

        /// <summary>ASCII 字符串</summary>
        A = 3,

        /// <summary>有符号 1 字节整型</summary>
        I1 = 4,

        /// <summary>有符号 2 字节整型</summary>
        I2 = 5,

        /// <summary>有符号 4 字节整型</summary>
        I4 = 6,

        /// <summary>有符号 8 字节整型</summary>
        I8 = 7,

        /// <summary>无符号 1 字节整型</summary>
        U1 = 8,

        /// <summary>无符号 2 字节整型</summary>
        U2 = 9,

        /// <summary>无符号 4 字节整型</summary>
        U4 = 10,

        /// <summary>无符号 8 字节整型</summary>
        U8 = 11,

        /// <summary>单精度浮点（4 字节）</summary>
        F4 = 12,

        /// <summary>双精度浮点（8 字节）</summary>
        F8 = 13
    }
}
