using System.Windows;
using System.Windows.Controls;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.ViewModels.Admin.Role;

namespace mcpx_template_project.Views.Pages.Admin.Role
{
    public partial class AddEditRoleWindow : Window
    {
        public AddEditRoleWindow(AddEditRoleViewModel viewModel)
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

        private void MenuCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is SysMenuVO menu)
            {
                if (DataContext is AddEditRoleViewModel vm)
                {
                    vm.OnMenuCheckedChanged(menu);
                }
            }
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditRoleViewModel vm)
            {
                vm.ConfirmCommand.Execute(null);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditRoleViewModel vm)
            {
                vm.CancelCommand.Execute(null);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is AddEditRoleViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
