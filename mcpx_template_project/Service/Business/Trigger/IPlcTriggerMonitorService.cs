using System.Collections.ObjectModel;
using mcpx_template_project.Domain.VO.Trigger;
using PlcTriggerEntity = mcpx_template_project.Domain.Entities.Business.Trigger.PlcTrigger;

namespace mcpx_template_project.Service.Business.Trigger
{
    /// <summary>
    /// PLC 触发器监控服务：统一处理警报与事件。
    /// 单线程串行轮询，从通讯引擎缓存（GetCachedPoints）读取点位值（不发 PLC 请求）。
    /// 按触发器配置的 trigger_mode 判定触发：
    ///   on     → 上升沿（OFF→ON）触发一次；
    ///   change → 值变化触发。
    /// 触发后按 trigger_type 派发动作：
    ///   alarm → 抛出 AlarmRaised 事件（供 UI 弹窗）；
    ///   event → 反射调用 action_key 对应的事件处理器。
    /// </summary>
    public interface IPlcTriggerMonitorService
    {
        /// <summary>当前正在发生的警报列表（trigger_type=alarm 且点位 ON 的触发器，供列表视图绑定）</summary>
        ObservableCollection<ActiveTriggerVO> ActiveAlarms { get; }

        /// <summary>当前正在发生的警报数量</summary>
        int ActiveAlarmCount { get; }

        /// <summary>警报触发时回调（参数为对应触发器配置）</summary>
        event Action<PlcTriggerEntity>? AlarmRaised;

        /// <summary>活跃警报集合发生变化时回调（用于主界面刷新警报数角标）</summary>
        event Action? ActiveAlarmsChanged;

        /// <summary>启动后台轮询（幂等）</summary>
        void Start();

        /// <summary>停止后台轮询</summary>
        void Stop();

        /// <summary>立即执行一次轮询（供手动刷新）</summary>
        Task PollOnceAsync();
    }
}
