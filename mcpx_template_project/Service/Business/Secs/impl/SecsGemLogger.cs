using Serilog;
using Secs4Net;
using Secs4Net.Sml;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// SECS/GEM 日志桥接：把 secs4net 的日志转到项目 Serilog，消息以 SML 格式输出。
    /// </summary>
    public sealed class SecsGemLogger : ISecsGemLogger
    {
        public void MessageIn(SecsMessage msg, int id)
            => Log.Information("[SECS] <-- 收 [0x{Id:X8}]{NewLine}{Sml}", id, Environment.NewLine, msg.ToSml());

        public void MessageOut(SecsMessage msg, int id)
            => Log.Information("[SECS] --> 发 [0x{Id:X8}]{NewLine}{Sml}", id, Environment.NewLine, msg.ToSml());

        public void Debug(string msg) => Log.Debug("[SECS] {Msg}", msg);

        public void Info(string msg) => Log.Information("[SECS] {Msg}", msg);

        public void Warning(string msg) => Log.Warning("[SECS] {Msg}", msg);

        public void Error(string msg) => Log.Error("[SECS] {Msg}", msg);

        public void Error(string msg, Exception ex) => Log.Error(ex, "[SECS] {Msg}", msg);

        public void Error(string msg, SecsMessage? message, Exception? ex)
            => Log.Error(ex, "[SECS] {Msg}{NewLine}{Sml}", msg, Environment.NewLine, message?.ToSml() ?? "(null)");
    }
}
