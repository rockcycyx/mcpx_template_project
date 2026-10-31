using System.Windows;
using mcpx_template_project.ViewModels.Business.Secs;

namespace mcpx_template_project.Views.Pages.Business.Secs
{
    /// <summary>
    /// 单个 SxFy 权限条目编辑弹窗（新增/编辑）。
    /// </summary>
    public partial class SecsSxFyFieldWindow : Window
    {
        public SecsSxFyFieldWindow(SecsSxFyFieldViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += OnRequestClose;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SecsSxFyFieldViewModel vm)
            {
                Title = vm.IsEditMode ? "编辑 SxFy 权限" : "新增 SxFy 权限";
            }
        }

        private void OnRequestClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as SecsSxFyFieldViewModel)?.ConfirmCommand.Execute(null);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as SecsSxFyFieldViewModel)?.CancelCommand.Execute(null);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is SecsSxFyFieldViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
