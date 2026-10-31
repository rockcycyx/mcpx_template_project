using Microsoft.Extensions.DependencyInjection;

namespace mcpx_template_project.DependencyInjection
{
    /// <summary>
    /// 批量业务后台服务注册器
    /// </summary>
    public static class ViewRegister
    {
        public static void AddViewsAndViewModels(this IServiceCollection services)
        {
            services.AddAdminViews();
            services.AddBusinessViews();
        }
    }
}
