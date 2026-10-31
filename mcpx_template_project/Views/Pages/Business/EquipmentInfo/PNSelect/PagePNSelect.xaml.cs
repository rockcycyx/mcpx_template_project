using System.Windows.Controls;
using mcpx_template_project.ViewModels.Business.EquipmentInfo.PNSelect;

namespace mcpx_template_project.Views.Pages.Business.EquipmentInfo.PNSelect
{
    /// <summary>
    /// PNSelect（配方选择）页面：配方 CRUD、下发到已连接 PLC、实时读回显示。
    /// </summary>
    public partial class PagePNSelect : Page
    {
        public PagePNSelect(PagePNSelectViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
