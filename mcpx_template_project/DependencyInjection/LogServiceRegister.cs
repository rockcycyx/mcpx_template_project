using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Settings.Configuration;
using mcpx_template_project.Core.Logging;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Admin.impl;

namespace mcpx_template_project.DependencyInjection
{
    /// <summary>
    /// 日志服务注册器
    /// </summary>
    /// <author>Rudeus</author>
    public static class LogServiceRegister
    {
        public static void AddLogServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 单文件发布后 Serilog 无法自动扫描程序集，需要显式指定包含 sink 的程序集
            var options = new ConfigurationReaderOptions(
                typeof(Serilog.FileLoggerConfigurationExtensions).Assembly);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration, options)
                .Enrich.FromLogContext()
                .CreateLogger();

            services.AddSingleton<ILogger>(Log.Logger);
            services.AddSingleton<ILogService, LogServiceImpl>();
            services.AddSingleton<IOperLogService, OperLogService>();
        }
    }
}
