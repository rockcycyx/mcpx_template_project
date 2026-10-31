using System.Windows;
using mcpx_template_project.ViewModels.Business.EquipmentInfo.PNSelect;

namespace mcpx_template_project.Views.Pages.Business.EquipmentInfo.PNSelect
{
    /// <summary>
    /// 配方编辑弹窗（仅数据库新增/编辑，不涉及 PLC）。
    /// </summary>
    public partial class RecipeEditWindow : Window
    {
        public RecipeEditWindow(RecipeEditViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += OnRequestClose;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RecipeEditViewModel vm)
            {
                Title = vm.IsEditMode ? "编辑配方" : "新增配方";
            }
        }

        private void OnRequestClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        private void ConfirmBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as RecipeEditViewModel)?.ConfirmCommand.Execute(null);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as RecipeEditViewModel)?.CancelCommand.Execute(null);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is RecipeEditViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
