using System.Windows.Controls;
using mcpx_template_project.ViewModels.Business.Secs;

namespace mcpx_template_project.Views.Pages.Business.Secs
{
    /// <summary>
    /// SECS/GEM 配置总览页：左侧 CEID→Report→VID 结构总图，右侧 Tab 分页签（VID/Report/Event）CRUD。
    /// </summary>
    public partial class PageSecsConfig : Page
    {
        public PageSecsConfig(PageSecsConfigViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += async (_, _) => await viewModel.InitializeAsync();
        }
    }
}
