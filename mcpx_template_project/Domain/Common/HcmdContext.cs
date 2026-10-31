namespace mcpx_template_project.Domain.Common
{
    /// <summary>
    /// Host 指令（HCMD）执行上下文：传入 [Hcmd] 方法的可选首参数。
    /// 携带当前 SECS 实例 ID、命令名（RCMD）与原始参数列表，便于命令方法按需取用。
    /// </summary>
    public sealed class HcmdContext
    {
        /// <summary>当前 SECS 实例 ID</summary>
        public long InstanceId { get; init; }

        /// <summary>命令名（RCMD，已转大写）</summary>
        public string Command { get; init; } = string.Empty;

        /// <summary>原始参数列表（CPNAME → 文本值，保持 Host 下发顺序）</summary>
        public IReadOnlyList<(string Name, string Value)> Parameters { get; init; } = Array.Empty<(string, string)>();

        /// <summary>按参数名（大小写不敏感）取值；不存在返回 null。</summary>
        public string? Get(string name)
        {
            foreach (var (n, v) in Parameters)
            {
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                    return v;
            }
            return null;
        }
    }
}
