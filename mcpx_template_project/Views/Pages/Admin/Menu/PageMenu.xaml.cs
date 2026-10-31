using System.Windows;
using System.Windows.Input;
using mcpx_template_project.ViewModels.Admin.Menu;

namespace mcpx_template_project.Views.Pages.Admin.Menu
{
    public partial class PageMenu
    {
        public PageMenu()
        {
            InitializeComponent();
            DataContext = (PageMenuViewModel)((App)Application.Current).Services!.GetService(typeof(PageMenuViewModel))!;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is PageMenuViewModel vm && e.NewValue is Domain.VO.SysMenuVO menu)
            {
                vm.SelectedMenu = menu;
            }
        }

        private void TxtMenuName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is PageMenuViewModel vm)
            {
                vm.SearchCommand.Execute(null);
            }
        }
    }
}
