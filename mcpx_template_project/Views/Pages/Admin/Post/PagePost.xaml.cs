using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using mcpx_template_project.ViewModels.Admin.Post;

namespace mcpx_template_project.Views.Pages.Admin.Post
{
    public partial class PagePost : Page
    {
        public PagePost()
        {
            InitializeComponent();
            DataContext = (PagePostViewModel)((App)Application.Current).Services!.GetService(typeof(PagePostViewModel))!;
        }

        private void TxtPostCode_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TriggerSearch();
                e.Handled = true;
            }
        }

        private void TxtPostName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TriggerSearch();
                e.Handled = true;
            }
        }

        private void TriggerSearch()
        {
            if (DataContext is PagePostViewModel vm)
            {
                vm.SearchCommand.Execute(null);
            }
        }
    }
}
