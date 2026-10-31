using Microsoft.Extensions.DependencyInjection;

namespace mcpx_template_project.Service.Admin.impl
{
    /// <summary>
    /// 基于 ActivatorUtilities 的 ViewModel 工厂实现：
    /// 显式参数按类型匹配构造函数，其余依赖从 DI 容器解析。
    /// </summary>
    public sealed class ViewModelFactory : IViewModelFactory
    {
        private readonly IServiceProvider _provider;

        public ViewModelFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public T Create<T>(params object?[] args)
        {
            return ActivatorUtilities.CreateInstance<T>(_provider, args!);
        }
    }
}
