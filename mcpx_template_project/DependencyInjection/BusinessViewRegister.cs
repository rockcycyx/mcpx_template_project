using Microsoft.Extensions.DependencyInjection;
using mcpx_template_project.ViewModels.Business;
using mcpx_template_project.ViewModels.Business.Dashboard;
using mcpx_template_project.ViewModels.Business.Monitor;
using mcpx_template_project.ViewModels.Business.EquipmentInfo.PNSelect;
using mcpx_template_project.ViewModels.Business.Trigger;
using mcpx_template_project.ViewModels.Business.SecsGem;
using mcpx_template_project.ViewModels.Business.Secs;
using mcpx_template_project.Views.Pages.Business;
using mcpx_template_project.Views.Pages.Business.Dashboard;
using mcpx_template_project.Views.Pages.Business.Monitor;
using mcpx_template_project.Views.Pages.Business.EquipmentInfo.PNSelect;
using mcpx_template_project.Views.Pages.Business.Trigger;
using mcpx_template_project.Views.Pages.Business.SecsGem;
using mcpx_template_project.Views.Pages.Business.Secs;


namespace mcpx_template_project.DependencyInjection
{
    /// <summary>
    /// 业务模块ViewModel&View注册器
    /// </summary>
    /// <author>Rudeus</author>
    public static class BusinessViewRegister
    {
        public static void AddBusinessViews(this IServiceCollection services)
        {
            services.AddTransient<PageHomeViewModel>();
            services.AddTransient<PageHome>();

            // 通讯设定模块：列表窗口及其 ViewModel 为单例（窗口关闭仅隐藏，生命周期不消亡）
            services.AddSingleton<CommSettingListViewModel>();
            services.AddSingleton<CommSettingWindow>();

            // 实时监控模块：窗口及 ViewModel 为单例（关闭仅隐藏）
            services.AddSingleton<PlcMonitorViewModel>();
            services.AddSingleton<PlcMonitorWindow>();

            // SECS/GEM 通讯设定模块：窗口及 ViewModel 为单例（关闭仅隐藏，生命周期不消亡）
            services.AddSingleton<SecsGemSettingViewModel>();
            services.AddSingleton<SecsGemSettingWindow>();

            // 配方选择（PNSelect）模块：页面及 ViewModel
            services.AddSingleton<PagePNSelectViewModel>();
            services.AddSingleton<PagePNSelect>();

            // PLC 警报页：展示当前正在发生的警报（页面每次导航创建，VM 依赖后台监控服务单例）
            services.AddTransient<PageAlarmViewModel>();
            services.AddTransient<PageAlarm>();

            // PLC 触发器 IO 设定页：警报+事件触发器配置 CRUD（页面每次导航创建）
            services.AddTransient<PageTriggerViewModel>();
            services.AddTransient<PageTrigger>();

            // SECS/GEM 配置总览页：左侧结构总图 + 右侧 Tab(VID/Report/Event/SxFy/HCMD) CRUD（页面每次导航创建）
            services.AddTransient<SecsVidTabViewModel>();
            services.AddTransient<SecsReportTabViewModel>();
            services.AddTransient<SecsEventTabViewModel>();
            services.AddTransient<SecsSxFyTabViewModel>();
            services.AddTransient<SecsHcmdTabViewModel>();
            services.AddTransient<PageSecsConfigViewModel>();
            services.AddTransient<PageSecsConfig>();

            // 生产看板页：KPI 指标卡 + 产量趋势/机台状态分布/机台产量图表（页面每次导航创建）
            services.AddTransient<PageDashboardViewModel>();
            services.AddTransient<PageDashboard>();

        }
    }
}
