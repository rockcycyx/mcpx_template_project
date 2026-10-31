using mcpx_template_project.Core.Handler;
using mcpx_template_project.Core.Logging;
namespace mcpx_template_project.Service.Business.Trigger.impl
{
    /// <summary>
    /// 示例事件处理器：触发时写一条操作日志（演示 IPlcEventHandler 的用法与注解维护）。
    /// 实际项目中可按业务需求新增更多处理器，每个处理器用 [PlcEventHandler] 特性声明 Key 与显示名，
    /// 配点界面的下拉框会自动读取这些处理器。
    /// </summary>
    [PlcEventHandler("log_event", "写入操作日志", Description = "触发时记录一条日志")]
    public class LogPlcEventHandler : IPlcEventHandler
    {
        private readonly ILogService _logService;

        public LogPlcEventHandler(ILogService logService)
        {
            _logService = logService;
        }

        public Task HandleAsync(PlcEventContext context)
        {
            _logService.LogInformation(
                "[PLC事件] {Name} 触发：点位 {Address}，当前值 {Value}，时间 {Time}",
                context.Name, context.FullAddress, context.CurrentValue, context.TriggerTime);

            return Task.CompletedTask;
        }
    }
}
