using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.ViewModels.Admin.Dept;

namespace mcpx_template_project.Views.Pages.Admin.Dept
{
    public partial class PageDept : Page
    {
        public PageDept()
        {
            InitializeComponent();
            DataContext = (PageDeptViewModel)((App)Application.Current).Services!.GetService(typeof(PageDeptViewModel))!;
        }

        private void TxtDeptName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TriggerSearch();
                e.Handled = true;
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is PageDeptViewModel vm && e.NewValue is SysDeptVO dept)
            {
                vm.SelectedDept = dept;
            }
        }

        private void TriggerSearch()
        {
            if (DataContext is PageDeptViewModel vm)
            {
                vm.SearchCommand.Execute(null);
            }
        }
    }
}
