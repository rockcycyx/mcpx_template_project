namespace mcpx_template_project.Domain.Entities.Business.Secs
{
    /// <summary>
    /// SECS/GEM VID 变量类别。
    /// </summary>
    public enum SecsVidType
    {
        /// <summary>状态变量（Status Variable，Host 只读，S1F3/S1F11）</summary>
        SVID = 0,

        /// <summary>数据变量（Data Variable，Host 可读写，S2F13/S2F15/S2F29）</summary>
        DVID = 1,

        /// <summary>设备常量（Equipment Constant，Host 只读，与 DVID 共用编号语义）</summary>
        ECID = 2
    }
}
