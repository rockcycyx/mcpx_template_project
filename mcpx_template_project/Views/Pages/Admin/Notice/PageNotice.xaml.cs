using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using mcpx_template_project.ViewModels.Admin.Notice;

namespace mcpx_template_project.Views.Pages.Admin.Notice
{
    public partial class PageNotice : Page
    {
        public PageNotice()
        {
            InitializeComponent();
            DataContext = (PageNoticeViewModel)((App)Application.Current).Services!.GetService(typeof(PageNoticeViewModel))!;
        }

        private void TxtTitle_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is PageNoticeViewModel vm)
                {
                    vm.SearchCommand.Execute(null);
                }
                e.Handled = true;
            }
        }
    }
}
