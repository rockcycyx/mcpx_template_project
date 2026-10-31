using Secs4Net;
using mcpx_template_project.Aspect.Attributes;

namespace mcpx_template_project.Core.SecsRoute
{
    /// <summary>
    /// 单个 SxF 消息处理器的委托签名。
    /// 返回 null 表示无需回复；返回非 null 时引擎负责 TryReplyAsync 回复给 Host。
    /// </summary>
    public delegate Task<SecsMessage?> SecsHandler(SecsMessage primaryMsg, CancellationToken ct);

    /// <summary>
    /// 一条已注册的路由：Stream + Function → 处理器委托。
    /// <see cref="Permission"/> 为方法上的 [SecsPermission] 注解（可为 null，表示无需前置权限校验）。
    /// </summary>
    public sealed record SecsRoute(byte Stream, byte Function, SecsHandler Handler, SecsPermissionAttribute? Permission)
    {
        public override string ToString() => $"S{Stream}F{Function}";
    }
}
