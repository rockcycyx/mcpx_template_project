namespace mcpx_template_project.Domain.Enums
{
    /// <summary>
    /// 膜宽范围枚举（对应 PLC 膜宽状态位）
    /// </summary>
    public enum MembraneWidthRange
    {
        /// <summary>&lt 0.6mm</summary>
        Below06mm = 0,

        /// <summary>&lt; 24mil</summary>
        Below24mil = 1,

        /// <summary>&gt;= 0.6mm</summary>
        AtLeast06mm = 2,

        /// <summary>&gt;= 24mil</summary>
        AtLeast24mil = 3
    }
}
