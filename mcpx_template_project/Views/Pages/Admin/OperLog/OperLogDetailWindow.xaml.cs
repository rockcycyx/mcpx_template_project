using System.Windows;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Views.Pages.Admin.OperLog
{
    public partial class OperLogDetailWindow : Window
    {
        public OperLogDetailWindow(SysOperLogVO log)
        {
            InitializeComponent();
            DataContext = log;
        }
    }
}
