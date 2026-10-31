using System.ComponentModel;
using System.Windows;
using mcpx_template_project.ViewModels.Business.SecsGem;

namespace mcpx_template_project.Views.Pages.Business.SecsGem
{
    /// <summary>
    /// SECS/GEM 通讯设定窗口。
    /// 生命周期不随窗口关闭而消亡：关闭按钮仅隐藏，配置与监听状态保持。
    /// </summary>
    public partial class SecsGemSettingWindow : Window
    {
        public SecsGemSettingWindow(SecsGemSettingViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // 关闭按钮仅隐藏窗口，保持生命周期
            e.Cancel = true;
            Hide();
            base.OnClosing(e);
        }
    }
}
