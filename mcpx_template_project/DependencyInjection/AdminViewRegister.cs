using Microsoft.Extensions.DependencyInjection;
using mcpx_template_project.ViewModels.Admin;
using mcpx_template_project.ViewModels.Admin.Dept;
using mcpx_template_project.ViewModels.Admin.Menu;
using mcpx_template_project.ViewModels.Admin.Notice;
using mcpx_template_project.ViewModels.Admin.OperLog;
using mcpx_template_project.ViewModels.Admin.Post;
using mcpx_template_project.ViewModels.Admin.Role;
using mcpx_template_project.ViewModels.Admin.User;
using mcpx_template_project.ViewModels.Admin.UserCenter;
using mcpx_template_project.Views.Pages.Admin;
using mcpx_template_project.Views.Pages.Admin.Dept;
using mcpx_template_project.Views.Pages.Admin.Menu;
using mcpx_template_project.Views.Pages.Admin.Notice;
using mcpx_template_project.Views.Pages.Admin.OperLog;
using mcpx_template_project.Views.Pages.Admin.Post;
using mcpx_template_project.Views.Pages.Admin.Role;
using mcpx_template_project.Views.Pages.Admin.User;
using mcpx_template_project.Views.Pages.Admin.UserCenter;

namespace mcpx_template_project.DependencyInjection
{
    /// <summary>
    /// 后台管理模块ViewModel&View注册器
    /// </summary>
    /// <author>Rudeus</author>
    public static class AdminViewRegister
    {
        public static void AddAdminViews(this IServiceCollection services)
        {
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<PageUserViewModel>();
            services.AddTransient<PageRoleVIewModel>();
            services.AddTransient<PageDeptViewModel>();
            services.AddTransient<PagePostViewModel>();
            services.AddTransient<PageNoticeViewModel>();
            services.AddTransient<PageMenuViewModel>();
            services.AddTransient<PageOperLogViewModel>();
            services.AddTransient<PageUserCenterViewModel>();

            services.AddTransient<MainWindow>();
            services.AddTransient<Login>();
            services.AddTransient<PageUser>();
            services.AddTransient<PageRole>();
            services.AddTransient<PageDept>();
            services.AddTransient<PagePost>();
            services.AddTransient<PageNotice>();
            services.AddTransient<PageMenu>();
            services.AddTransient<PageOperLog>();
            services.AddTransient<PageUserCenter>();
        }
    }
}
