using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace mcpx_template_project.Core.Logging
{
    /// <summary>
    /// 日志服务实现
    /// <author>Rudeus</author>
    /// </summary>
    public class LogServiceImpl : ILogService
    {
        private readonly ILogger _logger;

        public LogServiceImpl(ILogger logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message, params object[] args)
            => _logger.Information(message, args);

        public void LogWarning(string message, params object[] args)
            => _logger.Warning(message, args);

        public void LogError(Exception? exception, string message, params object[] args)
            => _logger.Error(exception, message, args);

        public void LogDebug(string message, params object[] args)
            => _logger.Debug(message, args);
    }
}
