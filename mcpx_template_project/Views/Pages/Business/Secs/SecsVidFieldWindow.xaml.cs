using System.Windows;
using mcpx_template_project.ViewModels.Business.Secs;

namespace mcpx_template_project.Views.Pages.Business.Secs
{
    /// <summary>
    /// 单个 VID 字段编辑弹窗（新增/编辑，含 [PlcRead] 注解字段下拉选择）。
    /// </summary>
    public partial class SecsVidFieldWindow : Window
    {
        public SecsVidFieldWindow(SecsVidFieldViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += OnRequestClose;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SecsVidFieldViewModel vm)
            {
                Title = vm.IsEditMode ? "编辑 VID" : "新增 VID";
            }
        }

        private void OnRequestClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as SecsVidFieldViewModel)?.ConfirmCommand.Execute(null);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as SecsVidFieldViewModel)?.CancelCommand.Execute(null);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is SecsVidFieldViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
