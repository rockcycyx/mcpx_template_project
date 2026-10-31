using System.Windows;
using mcpx_template_project.ViewModels.Admin.Notice;

namespace mcpx_template_project.Views.Pages.Admin.Notice
{
    public partial class NoticeDetailWindow : Window
    {
        public NoticeDetailWindow(NoticeDetailViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Title = "通知详情";

            viewModel.RequestClose += OnRequestClose;
        }

        private void OnRequestClose(bool result)
        {
            DialogResult = result;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is NoticeDetailViewModel vm)
            {
                vm.RequestClose -= OnRequestClose;
            }
            base.OnClosed(e);
        }
    }
}
