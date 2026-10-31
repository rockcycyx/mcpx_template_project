using System.Windows.Controls;
using mcpx_template_project.ViewModels.Business.Dashboard;

namespace mcpx_template_project.Views.Pages.Business.Dashboard
{
    /// <summary>
    /// 生产看板页：KPI 指标卡 + 产量趋势 / 机台状态分布 / 各机台今日产量图表。
    /// 实时数据取自 PLC 通讯缓存（2s 刷新），趋势图为示例数据。
    /// </summary>
    public partial class PageDashboard : Page
    {
        public PageDashboard(PageDashboardViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += async (_, _) => await viewModel.InitializeAsync();
        }
    }
}
