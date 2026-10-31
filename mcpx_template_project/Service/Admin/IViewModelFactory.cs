namespace mcpx_template_project.Service.Admin
{
    /// <summary>
    /// ViewModel 工厂抽象：由 DI 容器创建 ViewModel，
    /// 运行期数据作为显式参数按类型匹配构造函数，服务依赖由容器解析，
    /// 调用方无需手动 new 并传递依赖。
    /// </summary>
    public interface IViewModelFactory
    {
        /// <summary>
        /// 创建指定类型的 ViewModel。
        /// args 为运行期数据（如编辑对象），按类型匹配构造函数参数，
        /// 其余参数由 DI 容器解析。
        /// </summary>
        T Create<T>(params object?[] args);
    }
}
