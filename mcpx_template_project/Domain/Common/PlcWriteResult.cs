namespace mcpx_template_project.Domain.Common
{
    /// <summary>
    /// 通用 PLC 对象写入结果。
    /// </summary>
    public class PlcWriteResult
    {
        /// <summary>是否写入成功（收到 PLC 成功标志）</summary>
        public bool Success { get; set; }

        /// <summary>结果描述信息</summary>
        public string Message { get; set; } = string.Empty;
    }
}
