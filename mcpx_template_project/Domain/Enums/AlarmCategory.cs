namespace mcpx_template_project.Domain.Enums
{
    /// <summary>
    /// 警报类别（GEM Alarm Code Byte 的 bit7-1 部分，对齐 SEMI E5 / 文档 Alarm code byte 定义）。
    /// ALCD 字节 = bit8(发生/清除) + bit7-1(类别)，即 ALCD = (类别 &lt;&lt; 1) | (发生? 0x80 : 0x00)。
    /// 类别值 0~8，0 表示未使用。
    /// </summary>
    public enum AlarmCategory
    {
        /// <summary>0 - Not used（未使用）</summary>
        NotUsed = 0,

        /// <summary>1 - Personal safety（人身安全）</summary>
        PersonalSafety = 1,

        /// <summary>2 - Equipment safety（设备安全）</summary>
        EquipmentSafety = 2,

        /// <summary>3 - Parameter control warning（参数控制警告）</summary>
        ParameterControlWarning = 3,

        /// <summary>4 - Parameter control error（参数控制错误）</summary>
        ParameterControlError = 4,

        /// <summary>5 - Irrecoverable error（不可恢复错误）</summary>
        IrrecoverableError = 5,

        /// <summary>6 - Equipment status warning（设备状态警告）</summary>
        EquipmentStatusWarning = 6,

        /// <summary>7 - Attention flags（注意标志）</summary>
        AttentionFlags = 7,

        /// <summary>8 - Data integrity（数据完整性）</summary>
        DataIntegrity = 8,
    }
}
