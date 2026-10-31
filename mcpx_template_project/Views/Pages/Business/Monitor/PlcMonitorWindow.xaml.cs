using System.ComponentModel;
using System.Windows;
using mcpx_template_project.ViewModels.Business.Monitor;

namespace mcpx_template_project.Views.Pages.Business.Monitor
{
    /// <summary>
    /// 实时监控窗口：定时批量读取已连接通讯设定的所有PLC点位并实时展示。
    /// 生命周期不随窗口关闭而消亡：点击关闭按钮仅隐藏窗口并停止监控，
    /// 下次从右侧工具栏再次打开时复用同一实例。
    /// </summary>
    public partial class PlcMonitorWindow : Window
    {
        private readonly PlcMonitorViewModel _viewModel;

        public PlcMonitorWindow(PlcMonitorViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // 关闭按钮仅隐藏窗口，保持生命周期；同时停止监控释放Timer
            _viewModel.StopMonitorCommand.Execute(null);
            e.Cancel = true;
            Hide();
            base.OnClosing(e);
        }
    }
}
