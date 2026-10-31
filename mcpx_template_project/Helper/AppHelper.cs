using System.Net.Sockets;
using mcpx_template_project.Service.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace mcpx_template_project.Helper
{
    /// <summary>
    /// 应用工具类，提供当前用户信息和本机IP等通用方法
    /// </summary>
    /// <author>Rudeus</author>
    public static class AppHelper
    {
        private static IUserSessionService? _session;
        private static IServiceProvider? _services;

        /// <summary>
        /// 初始化工具类，在应用启动时调用一次
        /// </summary>
        public static void Initialize(IUserSessionService session, IServiceProvider services)
        {
            _session = session;
            _services = services;
        }

        /// <summary>
        /// 从全局DI容器获取服务（供AOP切面等无法构造注入的场景使用），容器未初始化时返回 null
        /// </summary>
        public static T? GetService<T>() where T : class
        {
            return _services?.GetService<T>();
        }

        /// <summary>
        /// 获取当前登录用户显示名（优先昵称，无昵称时回退账号），未登录时返回 "unknown"
        /// </summary>
        public static string GetCurrentUserName()
        {
            var user = _session?.CurrentUser;
            if (user == null) return "unknown";
            return user.NickName ?? user.UserName;
        }

        /// <summary>
        /// 获取当前登录用户昵称，未登录时返回 "unknown"
        /// </summary>
        public static string GetCurrentUserNickName()
        {
            return _session?.CurrentUser?.NickName ?? "unknown";
        }

        /// <summary>
        /// 获取本机IP地址
        /// </summary>
        public static string GetLocalIpAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is System.Net.IPEndPoint endPoint)
                {
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                // 获取失败时回退到默认值
            }
            return "unknown";
        }
    }
}