using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using mcpx_template_project.Configation;
using mcpx_template_project.DependencyInjection;
using mcpx_template_project.Helper;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.ViewModels.Admin;
using mcpx_template_project.Views.Pages.Admin;

namespace mcpx_template_project
{
    /// <summary>
    /// 应用程序入口类，全局IOC容器构建、配置加载、程序启动统一入口
    /// </summary>
    /// <author>Rudeus</author>
    public partial class App : Application
    {
        /// <summary>全局DI服务容器</summary>
        public IServiceProvider? Services { get; private set; }
        /// <summary>全局配置读取对象</summary>
        public IConfiguration? Configuration { get; private set; }

        /// <summary>
        /// 完成配置加载、依赖注册
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Configuration = AppConfiguration.BuildConfiguration()
                    ?? throw new InvalidOperationException("配置加载失败");

                // 注册依赖注入服务
                var services = new ServiceCollection();

                services.AddLogServices(Configuration);
                services.AddAppConfig(Configuration);

                string connStr = Configuration.GetConnectionString("SqliteDb")
                    ?? throw new InvalidOperationException("数据库连接字符串获取失败");
                services.AddDatabase(connStr);
                services.AddBusinessServices();
                services.AddViewsAndViewModels();

                Services = services.BuildServiceProvider();

                // 初始化工具类
                AppHelper.Initialize(Services.GetRequiredService<IUserSessionService>(), Services);

                // 扫描 PLC 字段目录（[PlcRead] 注解字段缓存，供 SECS VID 界面选择）
                Services.GetRequiredService<Core.Plc.PlcFieldCatalog>()
                    .Scan(System.Reflection.Assembly.GetEntryAssembly()!);

                // 自动建表并初始化种子数据
                DatabaseInitializer.Initialize(
                    Services.GetRequiredService<IDbContextFactory<EFDbContext>>(),
                    "resouces/Sql");

                var loginVm = Services.GetRequiredService<LoginViewModel>();
                var loginWindow = new Login(loginVm);
                loginWindow.ShowDialog();

                if (!loginVm.IsLoggedIn)
                {
                    Shutdown();
                    return;
                }

                var mainWindow = Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动异常: {ex.Message}\n\n堆栈:\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}
