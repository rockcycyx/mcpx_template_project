using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.Service.Admin.impl
{
    /// <summary>
    /// 导航服务实现：通过 DI 容器解析 Page，类型查找结果会被缓存。
    /// 优先使用 container.GetService，未注册时使用 ActivatorUtilities.CreateInstance 以支持构造函数注入。
    /// Page 实例生命周期由调用方（如 MainWindowViewModel.Tabs）管理，本服务不缓存 Page 实例。
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _provider;
        private readonly Dictionary<string, Type?> _typeCache = new();

        public NavigationService(IServiceProvider provider)
        {
            _provider = provider;
        }

        /// <inheritdoc/>
        public Page? CreatePage(string? componentFullName)
        {
            if (string.IsNullOrWhiteSpace(componentFullName)) return null;

            if (!_typeCache.TryGetValue(componentFullName, out var type))
            {
                type = Type.GetType(componentFullName)
                    ?? AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => a.GetType(componentFullName))
                        .FirstOrDefault(t => t != null);

                _typeCache[componentFullName] = type;
            }

            if (type == null || !typeof(Page).IsAssignableFrom(type)) return null;

            return (_provider.GetService(type) ?? ActivatorUtilities.CreateInstance(_provider, type)) as Page;
        }

        /// <inheritdoc/>
        public void ClearCache()
        {
        }
    }
}
