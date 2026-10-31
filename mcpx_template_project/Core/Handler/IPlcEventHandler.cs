using mcpx_template_project.Service.Business.Trigger;

namespace mcpx_template_project.Core.Handler
{
    /// <summary>
    /// PLC 事件处理器接口。
    /// 实现类通过 <see cref="PlcEventHandlerAttribute"/> 特性声明处理器 Key 与显示名，
    /// 启动时由注册表扫描 DI 容器中所有实现了本接口的类型并登记。
    /// 事件触发时，根据触发器表里配置的 action_key 反射调用对应的 HandleAsync。
    /// </summary>
    public interface IPlcEventHandler
    {
        /// <summary>
        /// 执行事件处理逻辑。
        /// </summary>
        /// <param name="context">触发上下文（含触发点信息、当前值、触发时间等）</param>
        Task HandleAsync(PlcEventContext context);
    }
}
