using System.Reflection;
using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Controller.SecsGem;
using mcpx_template_project.Core.Context;
using mcpx_template_project.Domain.Enums;
using mcpx_template_project.Service.Business.Secs;
using Secs4Net;
using Serilog;

namespace mcpx_template_project.Core.SecsRoute
{
    /// <summary>
    /// SECS 消息路由器：类 SpringMVC 的 DispatcherServlet。
    /// 启动时扫描所有 Controller 实例，读取 [S]/[F] 特性构建路由表；
    /// 运行时按 (S, F) 查表分发到对应方法。
    /// 分发前对带 [SecsPermission] 的方法做控制状态前置校验，拒绝时回 SxF+1（L,1 [U4 1]）。
    /// </summary>
    public sealed class SecsRouter
    {
        private readonly Dictionary<(byte Stream, byte Function), SecsRoute> _routes = new();
        private readonly ISecsPermissionService? _permissionService;
        private readonly ISecsContext? _secsContext;
        private readonly ISecsStateService? _stateService;

        public IReadOnlyCollection<SecsRoute> Routes => _routes.Values;

        /// <summary>
        /// 构造函数：注入所有 ISecsController 实现，扫描 [S]/[F] 特性构建路由表。
        /// 可选注入权限校验服务与上下文（用于 [SecsPermission] 前置增强）。
        /// </summary>
        public SecsRouter(
            IEnumerable<ISecsController> controllers,
            ISecsPermissionService? permissionService = null,
            ISecsContext? secsContext = null,
            ISecsStateService? stateService = null)
        {
            _permissionService = permissionService;
            _secsContext = secsContext;
            _stateService = stateService;
            RegisterControllers(controllers);
        }

        /// <summary>
        /// 扫描给定 Controller 实例集合，注册所有带 [S]/[F] 特性的方法。
        /// </summary>
        public void RegisterControllers(IEnumerable<ISecsController> controllers)
        {
            foreach (var controller in controllers)
            {
                RegisterController(controller);
            }

            Log.Information("[SECS] 路由表构建完成，共 {Count} 条路由", _routes.Count);
        }

        private void RegisterController(ISecsController controller)
        {
            var type = controller.GetType();
            var streamAttr = type.GetCustomAttribute<SecsStreamAttribute>();
            if (streamAttr is null)
            {
                return; // 未标注 [S] 的类跳过
            }

            byte stream = streamAttr.Stream;

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var funcAttr = method.GetCustomAttribute<SecsFunctionAttribute>();
                if (funcAttr is null) continue;

                byte function = funcAttr.Function;

                var handler = CreateHandler(controller, method);
                if (handler is null) continue;

                // 读取方法上的 [SecsPermission] 注解（可为 null = 无需前置校验）
                var permission = method.GetCustomAttribute<SecsPermissionAttribute>();

                var key = (stream, function);
                if (_routes.ContainsKey(key))
                {
                    Log.Warning("[SECS] 路由重复定义 S{S}F{F}，后者覆盖前者", stream, function);
                }

                _routes[key] = new SecsRoute(stream, function, handler, permission);
            }
        }

        /// <summary>
        /// 把 Controller 方法包装成 SecsHandler 委托，并做签名校验。
        /// </summary>
        private static SecsHandler? CreateHandler(object controller, MethodInfo method)
        {
            // 校验返回类型 Task<SecsMessage?> 与参数 (SecsMessage, CancellationToken)
            if (!typeof(Task<SecsMessage?>).IsAssignableFrom(method.ReturnType))
            {
                Log.Warning("[SECS] 跳过 {Type}.{Method}：返回类型应为 Task<SecsMessage?>",
                    method.DeclaringType?.Name, method.Name);
                return null;
            }

            var ps = method.GetParameters();
            if (ps.Length != 2 || ps[0].ParameterType != typeof(SecsMessage) || ps[1].ParameterType != typeof(CancellationToken))
            {
                Log.Warning("[SECS] 跳过 {Type}.{Method}：签名应为 (SecsMessage, CancellationToken)",
                    method.DeclaringType?.Name, method.Name);
                return null;
            }

            var func = method.CreateDelegate<Func<SecsMessage, CancellationToken, Task<SecsMessage?>>>(controller);
            return (msg, ct) => func(msg, ct);
        }

        /// <summary>
        /// 查找路由。未找到返回 null（调用方可据此回复 S9F5 等错误）。
        /// </summary>
        public bool TryGetRoute(byte stream, byte function, out SecsRoute route)
            => _routes.TryGetValue((stream, function), out route!);

        /// <summary>
        /// 判断是否存在指定 Stream 的任意路由（用于区分 S9F3 无法识别的 Stream vs S9F5 无法识别的 Function）。
        /// </summary>
        public bool HasStream(byte stream)
            => _routes.Keys.Any(k => k.Stream == stream);

        /// <summary>
        /// 分发主消息到对应 Controller 方法。
        /// 返回 null 表示无需回复；否则调用方负责回复。
        /// 路由不存在时返回 null（由调用方决定是否回 S9 错误）。
        /// </summary>
        public async Task<SecsMessage?> DispatchAsync(SecsMessage primaryMsg, CancellationToken ct)
        {
            if (!TryGetRoute(primaryMsg.S, primaryMsg.F, out var route))
            {
                Log.Warning("[SECS] 未找到处理器 S{S}F{F}", primaryMsg.S, primaryMsg.F);
                return null;
            }

            // [SecsPermission] 前置增强：打了注解才启用校验；分发前查表校验当前控制状态
            if (route.Permission is not null && _permissionService is not null)
            {
                var instanceId = _secsContext?.CurrentInstanceId ?? 0;
                var state = ResolveControlState(instanceId);

                if (!await _permissionService.IsAllowedAsync(route.Stream, route.Function, instanceId, state))
                {
                    Log.Warning("[SECS] 权限拒绝：S{S}F{F} 在当前控制状态 {State} 下不允许执行", route.Stream, route.Function, state);
                    return BuildRejectReply(primaryMsg);
                }
            }

            return await route.Handler(primaryMsg, ct).ConfigureAwait(false);
        }

        /// <summary>构造拒绝应答：SxF0（与请求同 Stream、Function 固定为 0），消息体为空（SecsItem 为 null）。</summary>
        private static SecsMessage BuildRejectReply(SecsMessage primaryMsg)
        {
            return new SecsMessage(primaryMsg.S, 0, replyExpected: false)
            {
                Name = "PermissionDenied",
                SecsItem = null
            };
        }

        /// <summary>解析当前实例的控制状态（状态机服务未注入时默认在线远程）。</summary>
        private SecsControlState ResolveControlState(long instanceId)
        {
            return _stateService?.GetControlState(instanceId) ?? SecsControlState.OnlineRemote;
        }
    }
}
