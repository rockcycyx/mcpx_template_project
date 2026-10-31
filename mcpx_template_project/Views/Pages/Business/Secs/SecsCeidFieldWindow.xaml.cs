using System.Windows;
using System.Windows.Controls;
using mcpx_template_project.ViewModels.Business.Secs;

namespace mcpx_template_project.Views.Pages.Business.Secs
{
    /// <summary>
    /// 单个 Event（CEID）字段编辑弹窗（新增/编辑，含成员 Report 多选）。
    /// </summary>
    public partial class SecsCeidFieldWindow : Window
    {
        public SecsCeidFieldWindow(SecsCeidFieldViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += OnRequestClose;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SecsCeidFieldViewModel vm)
            {
                Title = vm.IsEditMode ? "编辑 Event" : "新增 Event";
            }
        }

        private void OnRequestClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void ReportCheckBox_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as SecsCeidFieldViewModel)?.RecalculateOrders();
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as SecsCeidFieldViewModel)?.ConfirmCommand.Execute(null);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as SecsCeidFieldViewModel)?.CancelCommand.Execute(null);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is SecsCeidFieldViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
