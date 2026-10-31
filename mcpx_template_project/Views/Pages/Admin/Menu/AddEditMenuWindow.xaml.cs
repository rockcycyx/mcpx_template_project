using System.Windows;
using mcpx_template_project.ViewModels.Admin.Menu;

namespace mcpx_template_project.Views.Pages.Admin.Menu
{
    public partial class AddEditMenuWindow : Window
    {
        public AddEditMenuWindow(AddEditMenuViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += OnRequestClose;
        }

        private void OnRequestClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditMenuViewModel vm)
            {
                vm.ConfirmCommand.Execute(null);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditMenuViewModel vm)
            {
                vm.CancelCommand.Execute(null);
            }
        }

        private void SelectParentMenu_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditMenuViewModel vm)
            {
                var treeWindow = new MenuTreeSelectWindow(vm.MenuTree)
                {
                    Owner = this
                };

                if (vm.SelectedParentMenu != null)
                {
                    treeWindow.PreSelectMenu(vm.SelectedParentMenu.MenuId);
                }

                var result = treeWindow.ShowDialog();
                if (result == true && treeWindow.SelectedMenu != null)
                {
                    vm.SelectParentMenuCommand.Execute(treeWindow.SelectedMenu);
                }
            }
        }

        private void ClearParentMenu_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditMenuViewModel vm)
            {
                vm.ClearParentMenuCommand.Execute(null);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is AddEditMenuViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
