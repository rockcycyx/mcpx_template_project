namespace mcpx_template_project.Domain.Model
{
    /// <summary>
    /// SxFy 权限表查询条件。
    /// 各字段为 null 或空表示不参与过滤。
    /// </summary>
    public class SecsSxFyQuery
    {
        /// <summary>SxF 消息（如 "S2F41"，模糊匹配）</summary>
        public string? SxF { get; set; }

        /// <summary>OffLine（设备离线）状态是否允许（null=不限）</summary>
        public bool? Offline { get; set; }

        /// <summary>OnLine Local（在线-本地）状态是否允许（null=不限）</summary>
        public bool? OnlineLocal { get; set; }

        /// <summary>OnLine Remote（在线-远程）状态是否允许（null=不限）</summary>
        public bool? OnlineRemote { get; set; }
    }
}
