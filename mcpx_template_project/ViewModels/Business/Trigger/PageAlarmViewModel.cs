using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.VO.Trigger;
using mcpx_template_project.Service.Business.Trigger;

namespace mcpx_template_project.ViewModels.Business.Trigger
{
    /// <summary>
    /// 警报页视图模型：展示「当前正在发生的警报」（点位 ON 的 alarm 类型触发器）。
    /// 数据来自 <see cref="IPlcTriggerMonitorService.ActiveAlarms"/>，实时订阅后台监控服务。
    /// 触发器配置的增删改查已拆至独立页面 <see cref="PageTriggerViewModel"/>。
    /// </summary>
    public partial class PageAlarmViewModel : ObservableObject
    {
        private readonly IPlcTriggerMonitorService _monitorService;

        public ObservableCollection<ActiveTriggerVO> ActiveAlarms { get; } = new();

        [ObservableProperty] private string _summary = "暂无警报";

        /// <summary>当前警报数量（供界面显示）</summary>
        [ObservableProperty] private int _activeCount;

        public PageAlarmViewModel(IPlcTriggerMonitorService monitorService)
        {
            _monitorService = monitorService;

            _monitorService.ActiveAlarmsChanged += OnActiveAlarmsChanged;
            RefreshFromMonitor();
        }

        private void OnActiveAlarmsChanged()
        {
            RefreshFromMonitor();
        }

        /// <summary>从监控服务同步当前活跃警报集合。</summary>
        private void RefreshFromMonitor()
        {
            ActiveAlarms.Clear();
            foreach (var a in _monitorService.ActiveAlarms)
                ActiveAlarms.Add(a);

            ActiveCount = ActiveAlarms.Count;
            Summary = ActiveCount > 0 ? $"当前 {ActiveCount} 条警报正在发生" : "暂无警报";
        }

        /// <summary>手动刷新一次警报（立即轮询）。</summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await _monitorService.PollOnceAsync();
            RefreshFromMonitor();
        }
    }
}
