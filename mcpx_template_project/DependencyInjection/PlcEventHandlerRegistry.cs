using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using mcpx_template_project.Domain.VO.Trigger;
using mcpx_template_project.Core.Handler;

namespace mcpx_template_project.Service.Business.Trigger
{
    /// <summary>
    /// PLC 事件处理器注册表。
    /// 维护「处理器 Key → 处理器元数据」的映射，元数据包含显示名、描述与处理器类型。
    /// 提供：
    ///  1. <see cref="RegisterAll"/>：扫描装配中所有 <see cref="IPlcEventHandler"/> 实现并登记；
    ///  2. <see cref="GetOptions"/>：返回所有处理器选项（供配点界面下拉框展示）；
    ///  3. <see cref="ResolveHandler"/>：根据 Key 反射实例化处理器（通过 DI 构造函数解析）。
    /// </summary>
    public class PlcEventHandlerRegistry
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, PlcEventHandlerOption> _handlers = new(StringComparer.OrdinalIgnoreCase);

        public PlcEventHandlerRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            RegisterAll();
        }

        /// <summary>扫描装配中所有 IPlcEventHandler 实现，按特性 Key 登记。</summary>
        private void RegisterAll()
        {
            var type = typeof(IPlcEventHandler);
            var implementations = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch (ReflectionTypeLoadException) { return Type.EmptyTypes; }
                })
                .Where(t => type.IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

            foreach (var impl in implementations)
            {
                var attr = impl.GetCustomAttribute<PlcEventHandlerAttribute>();
                if (attr == null) continue;

                var option = new PlcEventHandlerOption
                {
                    Key = attr.Key,
                    DisplayName = attr.DisplayName,
                    Description = attr.Description,
                    HandlerType = impl
                };
                _handlers[attr.Key] = option;
            }
        }

        /// <summary>返回所有已注册处理器选项（按显示名排序），供下拉框绑定。</summary>
        public IReadOnlyList<PlcEventHandlerOption> GetOptions()
        {
            return _handlers.Values.OrderBy(o => o.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>判断指定 Key 是否已注册。</summary>
        public bool Contains(string? key)
        {
            return !string.IsNullOrWhiteSpace(key) && _handlers.ContainsKey(key);
        }

        /// <summary>根据 Key 从 DI 容器解析处理器实例。</summary>
        public IPlcEventHandler? ResolveHandler(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            if (!_handlers.TryGetValue(key, out var option)) return null;
            if (option.HandlerType == null) return null;

            return _serviceProvider.GetService(option.HandlerType) as IPlcEventHandler
                   ?? ActivatorUtilities.CreateInstance(_serviceProvider, option.HandlerType) as IPlcEventHandler;
        }
    }
}
