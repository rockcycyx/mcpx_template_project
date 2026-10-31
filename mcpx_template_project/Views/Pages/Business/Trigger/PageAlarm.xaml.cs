using System.Windows.Controls;
using mcpx_template_project.ViewModels.Business.Trigger;

namespace mcpx_template_project.Views.Pages.Business.Trigger
{
    /// <summary>
    /// 警报页：展示当前正在发生的警报列表（纯展示，触发器配置见 PageTrigger）。
    /// </summary>
    public partial class PageAlarm : Page
    {
        public PageAlarm(PageAlarmViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
