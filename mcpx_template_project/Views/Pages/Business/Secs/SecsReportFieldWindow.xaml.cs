using System.Windows;
using System.Windows.Controls;
using mcpx_template_project.ViewModels.Business.Secs;

namespace mcpx_template_project.Views.Pages.Business.Secs
{
    /// <summary>
    /// 单个 Report 字段编辑弹窗（新增/编辑，含成员 VID 多选）。
    /// </summary>
    public partial class SecsReportFieldWindow : Window
    {
        public SecsReportFieldWindow(SecsReportFieldViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += OnRequestClose;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SecsReportFieldViewModel vm)
            {
                Title = vm.IsEditMode ? "编辑 Report" : "新增 Report";
            }
        }

        private void OnRequestClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void VidCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // 勾选变化后重算选中顺序（勾选顺序即 sort）
            (DataContext as SecsReportFieldViewModel)?.RecalculateOrders();
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as SecsReportFieldViewModel)?.ConfirmCommand.Execute(null);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as SecsReportFieldViewModel)?.CancelCommand.Execute(null);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is SecsReportFieldViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
