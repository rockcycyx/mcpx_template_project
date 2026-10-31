using Microsoft.Extensions.DependencyInjection;

namespace mcpx_template_project.DependencyInjection
{
    /// <summary>
    /// 批量后台业务服务注册器
    /// <author>Rudeus</author>
    /// </summary>
    public static class ServiceRegister
    {
        public static void AddBusinessServices(this IServiceCollection services)
        {
            services.AddAdminServices();
            services.AddBusinessModuleServices();
        }
    }
}
