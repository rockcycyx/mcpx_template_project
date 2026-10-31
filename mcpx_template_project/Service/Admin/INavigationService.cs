using System.Windows.Controls;

namespace mcpx_template_project.Service.Admin
{
    /// <summary>
    /// 导航服务：根据 Page 类的全限定类型名解析 Page 实例。
    /// 优先从 DI 容器获取，失败时回退到支持依赖注入的反射创建，避免业务层直接使用反射。
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// 根据 Page 的全限定类型名获取 Page 实例（同一类型复用缓存实例，保留页面状态）
        /// </summary>
        /// <param name="componentFullName">Page 类的全限定类型名，兼容 "Namespace.ClassName" 和 "Namespace.ClassName, AssemblyName" 两种写法</param>
        /// <returns>Page 实例；若类型不存在或不可分配为 Page 则返回 null</returns>
        Page? CreatePage(string? componentFullName);

        /// <summary>
        /// 清空页面实例缓存，通常在用户登出时调用
        /// </summary>
        void ClearCache();
    }
}
