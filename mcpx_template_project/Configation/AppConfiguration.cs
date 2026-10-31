using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mcpx_template_project.Configation
{
    /// <summary>
    /// 公共配置类
    /// </summary>
    /// <author>Rudeus</author>
    public static class AppConfiguration
    {
        public static IConfiguration BuildConfiguration(string? basePath = null)
        {
            // 注册全局配置、强类型绑定配置
            var configBuilder = new ConfigurationBuilder();
            // 读取数据库连接字符，注册EF Sqlite上下文工厂
            configBuilder.SetBasePath(basePath ?? Directory.GetCurrentDirectory());

            configBuilder.AddJsonFile("resouces/appsettings.json", optional: false);

            return configBuilder.Build();
        }
    }
}
