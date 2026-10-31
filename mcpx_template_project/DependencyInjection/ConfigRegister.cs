using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Model.Secs;

namespace mcpx_template_project.DependencyInjection
{
    public static class ConfigRegister
    {
        /// <summary>
        /// 注册全局配置、强类型配置
        /// </summary>
        /// <author>Rudeus</author>
        public static void AddAppConfig(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton(config);
            services.Configure<ConnectionOption>(opt =>
            {
                config.GetSection("ConnectionStrings").Bind(opt);
            });

            // SECS/GEM 通讯配置：单例，生命周期独立于窗体（不随窗体关闭销毁），启动时从 appsettings.json 加载
            services.AddSingleton(sp =>
            {
                var cfg = new SecsGemConfiguration();
                new SecsGemConfigStore().Load(cfg);
                return cfg;
            });
            services.AddSingleton<SecsGemConfigStore>();
        }
    }
}