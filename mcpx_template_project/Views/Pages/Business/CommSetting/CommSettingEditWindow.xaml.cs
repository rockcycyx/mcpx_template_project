using System.Windows;
using mcpx_template_project.ViewModels.Business;

namespace mcpx_template_project.Views.Pages.Business
{
    /// <summary>
    /// 通讯设定编辑窗口（单头 + 单身明细）
    /// </summary>
    public partial class CommSettingEditWindow : Window
    {
        public CommSettingEditWindow(CommSettingEditViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += OnRequestClose;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CommSettingEditViewModel vm)
            {
                Title = vm.IsEditMode ? "编辑通讯设定" : "新增通讯设定";
            }
        }

        private void OnRequestClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as CommSettingEditViewModel)?.ConfirmCommand.Execute(null);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as CommSettingEditViewModel)?.CancelCommand.Execute(null);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is CommSettingEditViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
