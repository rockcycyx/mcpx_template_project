using Microsoft.Extensions.DependencyInjection;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Admin.impl;

namespace mcpx_template_project.DependencyInjection
{
    /// <summary>
    /// 系统服务注册类
    /// </summary>
    /// <author>Rudeus</author>
    public static class AdminServiceRegister
    {
        public static void AddAdminServices(this IServiceCollection services)
        {
            services.AddSingleton<IUserSessionService, UserSessionService>();
            services.AddScoped<IUserService, UserServiceImpl>();
            services.AddScoped<IAuthService, AuthServiceImpl>();
            services.AddScoped<ILicenseCheckService, LicenseCheckServiceImpl>();
            services.AddScoped<IRoleService, RoleServiceImpl>();
            services.AddScoped<IDeptService, DeptServiceImpl>();
            services.AddScoped<IPostService, PostServiceImpl>();
            services.AddScoped<INoticeService, NoticeServiceImpl>();
            services.AddScoped<IMenuService, MenuServiceImpl>();

            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IViewModelFactory, ViewModelFactory>();
            services.AddSingleton<INavigationService, NavigationService>();
        }
    }
}
