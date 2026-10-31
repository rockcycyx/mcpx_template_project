using System.Windows;
using mcpx_template_project.ViewModels.Business.Trigger;

namespace mcpx_template_project.Views.Pages.Business.Trigger
{
    /// <summary>
    /// 单个触发器字段编辑弹窗（新增/编辑触发器，含事件处理器下拉框）。
    /// </summary>
    public partial class TriggerFieldWindow : Window
    {
        public TriggerFieldWindow(TriggerFieldViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += OnRequestClose;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TriggerFieldViewModel vm)
            {
                Title = vm.IsEditMode ? "编辑触发器" : "新增触发器";
            }
        }

        private void OnRequestClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as TriggerFieldViewModel)?.ConfirmCommand.Execute(null);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as TriggerFieldViewModel)?.CancelCommand.Execute(null);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is TriggerFieldViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
