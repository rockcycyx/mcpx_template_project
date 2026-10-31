using System.Globalization;
using System.Reflection;
using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Controller.SecsGem;
using mcpx_template_project.Domain.Common;
using Serilog;

namespace mcpx_template_project.Core.SecsRoute
{
    /// <summary>
    /// 单个 Host 指令（HCMD）的参数描述（供配置界面展示）。
    /// </summary>
    public sealed class HcmdParameterDescriptor
    {
        /// <summary>参数名（CPNAME）</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>参数 CLR 类型</summary>
        public Type Type { get; init; } = typeof(string);

        /// <summary>是否可选（有默认值或可空）</summary>
        public bool IsOptional { get; init; }

        /// <summary>显示文本</summary>
        public string Display => $"{Name} ({TypeName})";

        private string TypeName => Type switch
        {
            var t when t == typeof(string) => "string",
            var t when t == typeof(int) => "int",
            var t when t == typeof(long) => "long",
            var t when t == typeof(double) => "double",
            var t when t == typeof(bool) => "bool",
            _ => Type.Name,
        };
    }

    /// <summary>
    /// 单个已注册的 Host 指令（HCMD）描述（供配置界面展示 + 运行时调用）。
    /// </summary>
    public sealed class HcmdDescriptor
    {
        /// <summary>命令名（RCMD，大写）</summary>
        public string CommandName { get; init; } = string.Empty;

        /// <summary>承载方法的处理器实例（单例）</summary>
        public IHostCommandHandler Handler { get; init; } = null!;

        /// <summary>承载方法的类型</summary>
        public Type DeclaringType { get; init; } = null!;

        /// <summary>方法名</summary>
        public string MethodName { get; init; } = string.Empty;

        /// <summary>方法元数据（用于反射调用）</summary>
        public MethodInfo Method { get; init; } = null!;

        /// <summary>是否有 HcmdContext 首参数</summary>
        public bool HasContextParam { get; init; }

        /// <summary>业务参数描述（不含 HcmdContext）</summary>
        public IReadOnlyList<HcmdParameterDescriptor> Parameters { get; init; } = Array.Empty<HcmdParameterDescriptor>();

        /// <summary>签名展示，如 "PP_SELECT(EquipID, PPID, LotID, WaferCnt)"</summary>
        public string Signature => $"{CommandName}({string.Join(", ", Parameters.Select(p => p.Name))})";
    }

    /// <summary>
    /// Host 指令（HCMD）注册表：扫描所有 <see cref="IHostCommandHandler"/> 实现中带 [Hcmd] 注解的方法，
    /// 构建「命令名 → 命令描述」映射；运行时按 RCMD 查找并反射调用，按参数名注入 S2F41 参数值。
    /// </summary>
    public sealed class HcmdRegistry
    {
        private readonly Dictionary<string, HcmdDescriptor> _commands = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>所有已注册命令（按命令名排序）</summary>
        public IReadOnlyList<HcmdDescriptor> Commands => _commands.Values
            .OrderBy(c => c.CommandName, StringComparer.Ordinal)
            .ToList();

        public HcmdRegistry(IEnumerable<IHostCommandHandler> handlers)
        {
            RegisterHandlers(handlers);
        }

        /// <summary>按命令名（大小写不敏感）查找命令。</summary>
        public bool TryGet(string commandName, out HcmdDescriptor descriptor)
            => _commands.TryGetValue(commandName, out descriptor!);

        private void RegisterHandlers(IEnumerable<IHostCommandHandler> handlers)
        {
            foreach (var handler in handlers)
            {
                var type = handler.GetType();
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    var attr = method.GetCustomAttribute<HcmdAttribute>();
                    if (attr is null) continue;

                    // 校验返回类型为 Task（异步）
                    if (!typeof(Task).IsAssignableFrom(method.ReturnType))
                    {
                        Log.Warning("[HCMD] 跳过 {Type}.{Method}：返回类型应为 Task",
                            type.Name, method.Name);
                        continue;
                    }

                    string commandName = string.IsNullOrEmpty(attr.CommandName)
                        ? method.Name.ToUpperInvariant()
                        : attr.CommandName;

                    var ps = method.GetParameters();
                    bool hasContext = ps.Length > 0 && ps[0].ParameterType == typeof(HcmdContext);
                    int start = hasContext ? 1 : 0;

                    var parameters = new List<HcmdParameterDescriptor>();
                    for (int i = start; i < ps.Length; i++)
                    {
                        var p = ps[i];
                        parameters.Add(new HcmdParameterDescriptor
                        {
                            Name = p.Name ?? string.Empty,
                            Type = p.ParameterType,
                            IsOptional = p.IsOptional || Nullable.GetUnderlyingType(p.ParameterType) is not null,
                        });
                    }

                    if (_commands.ContainsKey(commandName))
                    {
                        Log.Warning("[HCMD] 命令 {Command} 重复定义（{Type}.{Method}），后者覆盖前者",
                            commandName, type.Name, method.Name);
                    }

                    _commands[commandName] = new HcmdDescriptor
                    {
                        CommandName = commandName,
                        Handler = handler,
                        DeclaringType = type,
                        MethodName = method.Name,
                        Method = method,
                        HasContextParam = hasContext,
                        Parameters = parameters,
                    };
                }
            }

            Log.Information("[HCMD] Host 指令注册完成，共 {Count} 条命令", _commands.Count);
        }

        /// <summary>
        /// 调用指定命令：按参数名把 <paramref name="parameters"/> 注入到方法入参，反射执行。
        /// </summary>
        /// <param name="instanceId">当前 SECS 实例 ID</param>
        /// <param name="commandName">命令名（RCMD）</param>
        /// <param name="parameters">参数列表（CPNAME → 文本值）</param>
        /// <returns>执行结果：成功返回 (true, null)；失败返回 (false, 错误描述)。</returns>
        public async Task<(bool Ok, string? Error)> InvokeAsync(
            long instanceId, string commandName, IReadOnlyList<(string Name, string Value)> parameters)
        {
            if (!_commands.TryGetValue(commandName, out var desc))
            {
                return (false, $"命令 {commandName} 未注册");
            }

            var ctx = new HcmdContext
            {
                InstanceId = instanceId,
                Command = commandName,
                Parameters = parameters,
            };

            var args = new object?[desc.Parameters.Count + (desc.HasContextParam ? 1 : 0)];
            int idx = 0;
            if (desc.HasContextParam)
            {
                args[idx++] = ctx;
            }

            foreach (var p in desc.Parameters)
            {
                var value = FindParameter(parameters, p.Name);
                if (value is null)
                {
                    if (p.IsOptional)
                    {
                        args[idx++] = GetDefault(p.Type);
                        continue;
                    }
                    return (false, $"缺少参数 {p.Name}");
                }

                if (!TryConvert(value, p.Type, out var converted))
                {
                    return (false, $"参数 {p.Name} 的值 \"{value}\" 无法转换为 {p.Type.Name}");
                }

                args[idx++] = converted;
            }

            try
            {
                var task = (Task)desc.Method.Invoke(desc.Handler, args)!;
                await task.ConfigureAwait(false);
                return (true, null);
            }
            catch (TargetInvocationException tie)
            {
                var ex = tie.InnerException ?? tie;
                Log.Error(ex, "[HCMD] 命令 {Command} 执行异常", commandName);
                return (false, ex.Message);
            }
        }

        private static string? FindParameter(IReadOnlyList<(string Name, string Value)> parameters, string name)
        {
            foreach (var (n, v) in parameters)
            {
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                    return v;
            }
            return null;
        }

        private static bool TryConvert(string text, Type target, out object? result)
        {
            target = Nullable.GetUnderlyingType(target) ?? target;
            try
            {
                if (target == typeof(string))
                {
                    result = text;
                    return true;
                }
                if (target == typeof(int))
                {
                    result = int.Parse(text, CultureInfo.InvariantCulture);
                    return true;
                }
                if (target == typeof(long))
                {
                    result = long.Parse(text, CultureInfo.InvariantCulture);
                    return true;
                }
                if (target == typeof(double))
                {
                    result = double.Parse(text, CultureInfo.InvariantCulture);
                    return true;
                }
                if (target == typeof(float))
                {
                    result = float.Parse(text, CultureInfo.InvariantCulture);
                    return true;
                }
                if (target == typeof(bool))
                {
                    result = text is "1" or "true" or "TRUE" or "True" or "yes" or "YES" || bool.TryParse(text, out var b) && b;
                    return true;
                }
                if (target == typeof(short))
                {
                    result = short.Parse(text, CultureInfo.InvariantCulture);
                    return true;
                }
                if (target == typeof(byte))
                {
                    result = byte.Parse(text, CultureInfo.InvariantCulture);
                    return true;
                }
                if (target == typeof(DateTime))
                {
                    result = DateTime.Parse(text, CultureInfo.InvariantCulture);
                    return true;
                }
                if (target.IsEnum)
                {
                    result = Enum.Parse(target, text, ignoreCase: true);
                    return true;
                }
            }
            catch
            {
                // 转换失败，落到下方返回 false
            }

            result = null;
            return false;
        }

        private static object? GetDefault(Type type)
        {
            if (!type.IsValueType || Nullable.GetUnderlyingType(type) is not null)
                return null;
            return Activator.CreateInstance(type);
        }
    }
}
