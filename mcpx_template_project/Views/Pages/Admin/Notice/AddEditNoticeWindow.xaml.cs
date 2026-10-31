using System.Windows;
using mcpx_template_project.ViewModels.Admin.Notice;

namespace mcpx_template_project.Views.Pages.Admin.Notice
{
    public partial class AddEditNoticeWindow : Window
    {
        public AddEditNoticeWindow(AddEditNoticeViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += OnRequestClose;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditNoticeViewModel vm)
            {
                Title = vm.IsEditMode ? "修改通知" : "添加通知";
            }
        }

        private void OnRequestClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditNoticeViewModel vm)
            {
                vm.ConfirmCommand.Execute(null);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditNoticeViewModel vm)
            {
                vm.CancelCommand.Execute(null);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is AddEditNoticeViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
