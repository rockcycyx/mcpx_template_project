using System.Windows;
using mcpx_template_project.ViewModels.Admin.Post;

namespace mcpx_template_project.Views.Pages.Admin.Post
{
    public partial class AddEditPostWindow : Window
    {
        public AddEditPostWindow(AddEditPostViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += OnRequestClose;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditPostViewModel vm)
            {
                Title = vm.IsEditMode ? "修改岗位" : "添加岗位";
            }
        }

        private void OnRequestClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditPostViewModel vm)
            {
                vm.ConfirmCommand.Execute(null);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditPostViewModel vm)
            {
                vm.CancelCommand.Execute(null);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is AddEditPostViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
