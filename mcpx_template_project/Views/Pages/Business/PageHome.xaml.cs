using System.Windows.Controls;
using mcpx_template_project.ViewModels.Business;

namespace mcpx_template_project.Views.Pages.Business
{
    /// <summary>
    /// PageHome.xaml 的交互逻辑
    /// </summary>
    public partial class PageHome : Page
    {
        public PageHome(PageHomeViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
