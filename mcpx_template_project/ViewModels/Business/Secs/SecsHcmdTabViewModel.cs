using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using mcpx_template_project.Core.SecsRoute;

namespace mcpx_template_project.ViewModels.Business.Secs
{
    /// <summary>
    /// HCMD（Host Command）Tab 视图模型：展示所有已注册的 Host 指令（[Hcmd] 注解扫描而来）。
    /// 开发者只需在实现了 IHostCommandHandler 的类方法上打 [Hcmd]，本页即可自动列出，
    /// 供查看每条命令的命令名、承载类型与参数签名（参数名即 S2F41 的 CPNAME）。
    /// </summary>
    public partial class SecsHcmdTabViewModel : ObservableObject
    {
        private readonly HcmdRegistry _hcmdRegistry;

        public ObservableCollection<HcmdDescriptor> Commands { get; } = new();

        /// <summary>当前实例 ID（由主 VM 设置）</summary>
        public long InstanceId { get; set; }

        [ObservableProperty] private string _summary = string.Empty;

        public SecsHcmdTabViewModel(HcmdRegistry hcmdRegistry)
        {
            _hcmdRegistry = hcmdRegistry;
        }

        public async Task LoadAsync()
        {
            Commands.Clear();
            foreach (var cmd in _hcmdRegistry.Commands)
                Commands.Add(cmd);

            Summary = $"共 {Commands.Count} 条 Host 指令（S2F41 Remote Command）";
            await Task.CompletedTask;
        }
    }
}
