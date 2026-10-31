using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using mcpx_template_project.Configation;

namespace mcpx_template_project.DependencyInjection
{
    /// <summary>
    /// 数据库服务注册器
    /// <author>Rudeus</author>
    /// </summary>
    public static class DbRegister
    {
        public static void AddDatabase(this IServiceCollection services, string connStr)
        {
            string resolved = ResolveConnectionString(connStr);
            services.AddDbContextFactory<EFDbContext>(options =>
                options.UseSqlite(resolved));
        }

        /// <summary>
        /// 解析连接字符串中的 |DataDirectory| 占位符为当前工作目录
        /// </summary>
        public static string ResolveConnectionString(string? connStr)
        {
            if (string.IsNullOrWhiteSpace(connStr))
                throw new ArgumentException("数据库连接字符串不能为空", nameof(connStr));
            return connStr.Replace("|DataDirectory|", Directory.GetCurrentDirectory());
        }
    }
}