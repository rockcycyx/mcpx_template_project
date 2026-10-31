using System.Windows;
using System.Windows.Controls;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.ViewModels.Admin.Dept;

namespace mcpx_template_project.Views.Pages.Admin.Dept
{
    public partial class AddDeptWindow : Window
    {
        public AddDeptWindow(AddDeptViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += OnRequestClose;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddDeptViewModel vm)
            {
                Title = vm.IsEditMode ? "修改部门" : "添加部门";

                if (vm.IsEditMode && vm.EditingDept != null && vm.EditingDept.ParentId.HasValue)
                {
                    vm.FindAndSelectParent(vm.EditingDept.ParentId.Value);
                }
            }
        }

        private void DeptTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is AddDeptViewModel vm && e.NewValue is SysDeptVO dept)
            {
                vm.SelectParentDeptCommand.Execute(dept);
            }
        }

        private void OnRequestClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddDeptViewModel vm)
            {
                vm.ConfirmCommand.Execute(null);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddDeptViewModel vm)
            {
                vm.CancelCommand.Execute(null);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is AddDeptViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
