using System.Windows;
using System.Windows.Controls;
using mcpx_template_project.ViewModels.Admin.OperLog;

namespace mcpx_template_project.Views.Pages.Admin.OperLog
{
    public partial class PageOperLog : Page
    {
        public PageOperLog()
        {
            InitializeComponent();
            DataContext = (PageOperLogViewModel)((App)Application.Current).Services!.GetService(typeof(PageOperLogViewModel))!;
        }
    }
}
