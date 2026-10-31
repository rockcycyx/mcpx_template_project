using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using mcpx_template_project.ViewModels.Admin.User;

namespace mcpx_template_project.Views.Pages.Admin.User
{
    public partial class PageUser : Page
    {
        public PageUser()
        {
            InitializeComponent();
            DataContext = (PageUserViewModel)((App)Application.Current).Services!.GetService(typeof(PageUserViewModel))!;
        }

        private void TxtUserName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TriggerSearch();
                e.Handled = true;
            }
        }

        private void TxtPhone_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TriggerSearch();
                e.Handled = true;
            }
        }

        private void TriggerSearch()
        {
            if (DataContext is PageUserViewModel vm)
            {
                vm.SearchCommand.Execute(null);
            }
        }
    }
}
