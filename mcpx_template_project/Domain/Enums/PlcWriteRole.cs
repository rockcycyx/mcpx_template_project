namespace mcpx_template_project.Domain.Enums
{
    /// <summary>
    /// PLC 写点位的语义角色。
    /// 用于在扫描阶段自动把写点位分类，业务代码无需再按属性名二次区分。
    /// </summary>
    public enum PlcWriteRole
    {
        /// <summary>普通值点位：承载业务数据下发。</summary>
        Value = 0,

        /// <summary>写入触发位：置 1 通知 PLC 开始处理本对象写入。</summary>
        Trigger = 1,

        /// <summary>回复位：PLC 回写三态值（0=无回应/处理中，1=ACK，2=NAK）。</summary>
        Reply = 2
    }
}
