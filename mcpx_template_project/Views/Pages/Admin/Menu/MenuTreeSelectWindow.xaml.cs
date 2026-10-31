using System.Collections.ObjectModel;
using System.Windows;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Views.Pages.Admin.Menu
{
    public partial class MenuTreeSelectWindow : Window
    {
        public SysMenuVO? SelectedMenu { get; private set; }

        public MenuTreeSelectWindow(ObservableCollection<SysMenuVO> menuTree)
        {
            InitializeComponent();
            MenuTreeView.ItemsSource = menuTree;
        }

        public void PreSelectMenu(long menuId)
        {
            SelectedMenu = FindMenuById(MenuTreeView.Items.Cast<SysMenuVO>(), menuId);
        }

        private void MenuTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is SysMenuVO menu)
            {
                SelectedMenu = menu;
            }
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = SelectedMenu != null;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            SelectedMenu = null;
            DialogResult = false;
            Close();
        }

        private static SysMenuVO? FindMenuById(IEnumerable<SysMenuVO> nodes, long menuId)
        {
            foreach (var node in nodes)
            {
                if (node.MenuId == menuId) return node;
                var found = FindMenuById(node.Children, menuId);
                if (found != null) return found;
            }
            return null;
        }
    }
}
