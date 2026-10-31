namespace mcpx_template_project.Aspect.Attributes
{
    /// <summary>
    /// 标记一个 Controller 类所处理的 SECS Stream（1~127）。
    /// 等价于 SpringMVC 的类级 @RequestMapping，此处表示消息的 Stream 维度。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SecsStreamAttribute : Attribute
    {
        public byte Stream { get; }

        public SecsStreamAttribute(byte stream)
        {
            Stream = stream;
        }
    }

    /// <summary>
    /// 标记 Controller 中某个方法所处理的 SECS Function（0~255）。
    /// 等价于 SpringMVC 的方法级 @RequestMapping，此处表示消息的 Function 维度。
    /// 方法签名约束：Task&lt;SecsMessage?&gt; Xxx(SecsMessage primaryMsg, CancellationToken ct)。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class SecsFunctionAttribute : Attribute
    {
        public byte Function { get; }

        public SecsFunctionAttribute(byte function)
        {
            Function = function;
        }
    }

    /// <summary>
    /// SECS 消息执行权限前置增强标记（方法级，独立于 <see cref="SecsFunctionAttribute"/>，职责单一）。
    /// 打在带 [SecsFunction] 的方法上，作为「该 SxF 需要权限前置校验」的开关：
    ///   - 未打本注解 → 路由分发不查表、直接放行（权限表对未标注的方法不生效）；
    ///   - 打了本注解 → 路由分发前查 secs_sxfy 表校验当前控制状态；
    ///     表中存在启用条目 → 以表为准判定；表中无条目 → 默认拦截（拒绝执行）。
    ///
    /// 权限矩阵（OffLine / OnLine Local / OnLine Remote 各状态是否允许）完全由 secs_sxfy 表决定，
    /// 本注解不再承载任何默认允许位（即「打了注解但未配置表」一律拦截）。
    /// 拒绝时由路由器回复 SxF（与请求同 Stream/Function）且消息体为空的应答。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class SecsPermissionAttribute : Attribute
    {
    }

    /// <summary>
    /// Host 指令（HCMD）标记：打在实现了 <see cref="Controller.SecsGem.IHostCommandHandler"/> 的类的方法上，
    /// 声明「该方法可被 Host 通过 S2F41 Remote Command 调用」。
    /// 命令名（RCMD）默认取方法名（转大写）；方法参数名即 S2F41 参数列表中的 CPNAME，
    /// 运行时按参数名（大小写不敏感）匹配把参数值注入到对应入参。
    ///
    /// 方法签名约定：
    ///   Task Xxx(HcmdContext ctx, ...)   —— 首个参数可为 <see cref="Domain.Common.HcmdContext"/>（可选），
    ///                                         用于获取 instanceId 与原始参数；
    ///   其余参数按参数名匹配 S2F41 的 &lt;A "CPNAME"&gt; 注入，类型支持 string/int/long/double/bool 等。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class HcmdAttribute : Attribute
    {
        /// <summary>命令名（RCMD）；未指定时默认取方法名（转大写）。</summary>
        public string CommandName { get; }

        public HcmdAttribute(string? commandName = null)
        {
            CommandName = (commandName ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
