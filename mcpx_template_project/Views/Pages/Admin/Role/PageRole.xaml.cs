using System.Windows;
using System.Windows.Controls;
using mcpx_template_project.ViewModels.Admin.Role;

namespace mcpx_template_project.Views.Pages.Admin.Role
{
    public partial class PageRole : Page
    {
        public PageRole()
        {
            InitializeComponent();
            DataContext = (PageRoleVIewModel)((App)Application.Current).Services!.GetService(typeof(PageRoleVIewModel))!;
        }
    }
}
