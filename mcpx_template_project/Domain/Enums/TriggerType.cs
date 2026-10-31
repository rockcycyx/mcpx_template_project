namespace mcpx_template_project.Domain.Enums
{
    /// <summary>
    /// PLC 触发器类型：区分「警报」与「事件」两类动作。
    /// 警报 = 弹窗提示；事件 = 反射调用已注册的处理器方法。
    /// </summary>
    public enum TriggerType
    {
        /// <summary>警报：触发时弹窗提示（MessageBox + 响铃）</summary>
        Alarm = 0,

        /// <summary>事件：触发时反射调用已注册的事件处理器</summary>
        Event = 1
    }
}
