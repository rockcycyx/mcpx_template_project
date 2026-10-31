using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mcpx_template_project.Core.Logging
{
    /// <summary>
    /// 抽象日志服务，使拦截器不直接依赖Serilog
    /// <author>Rudeus</author>
    /// </summary>
    public interface ILogService
    {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(Exception? exception, string message, params object[] args);
        void LogDebug(string message, params object[] args);
    }
}
