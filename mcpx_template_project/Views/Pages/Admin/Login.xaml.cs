using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Input;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.ViewModels.Admin;
using mcpx_template_project.Helper;

namespace mcpx_template_project.Views.Pages.Admin
{
    public partial class Login : Window
    {
        public Login(LoginViewModel loginViewModel)
        {
            InitializeComponent();
            this.DataContext = loginViewModel;
            WeakReferenceMessenger.Default.Register<LoginSuccessMessage>(this, (recipient, message) =>
            {
                this.Close();
            });
            WeakReferenceMessenger.Default.Register<LogoutMessage>(this, (recipient, message) =>
            {
                this.Close();
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // 补回 WS_THICKFRAME（ResizeMode=NoResize 会移除它导致 DWM 亚克力不绘制），
            // 并让 DWM 背景材质跟随应用明暗主题（否则深色系统下背景发白）。
            DwmWindowHelper.ApplyWindowBackdrop(this);
        }

        private void WindowDrag_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
