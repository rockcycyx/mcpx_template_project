using System.Windows;
using System.Windows.Controls;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.ViewModels.Admin.User;

namespace mcpx_template_project.Views.Pages.Admin.User
{
    public partial class EditUserWindow : Window
    {
        public EditUserWindow(EditUserViewModel viewModel)
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

        private void DeptTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is EditUserViewModel vm && e.NewValue is SysDeptVO dept)
            {
                vm.SelectDeptCommand.Execute(dept);
            }
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditUserViewModel vm)
            {
                vm.ConfirmCommand.Execute(null);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is EditUserViewModel vm)
            {
                vm.CancelCommand.Execute(null);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is EditUserViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
