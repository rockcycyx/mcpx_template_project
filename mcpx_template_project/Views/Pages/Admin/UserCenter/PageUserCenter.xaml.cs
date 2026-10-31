using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using mcpx_template_project.ViewModels.Admin.UserCenter;

namespace mcpx_template_project.Views.Pages.Admin.UserCenter
{
    public partial class PageUserCenter : Page
    {
        // Copilot sprite 长图参数：Assets/images/Copilot_sprite.png 为 128x6912（54 帧垂直排列，每帧 128x128，4 倍清晰度）
        // 显示时整图缩放到宽 80（高 4320），每帧显示高度 80，位移窗口 80x80
        private const int AvatarFrameCount = 54;
        private const double AvatarFrameDisplaySize = 96;
        private const double AvatarFrameIntervalMs = 60; // 54 帧约 3.2 秒一轮；想更慢就调大

        private DispatcherTimer? _avatarTimer;
        private int _avatarFrame;

        public PageUserCenter()
        {
            InitializeComponent();
            DataContext = (PageUserCenterViewModel)((App)Application.Current).Services!.GetService(typeof(PageUserCenterViewModel))!;
            this.Loaded += PageUserCenter_Loaded;
            this.Unloaded += (s, e) =>
            {
                _avatarTimer?.Stop();
                _avatarTimer = null; // 置空，保证切回页面时能重新启动动画
            };
        }

        private void PageUserCenter_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is PageUserCenterViewModel vm)
            {
                if (vm.Sex == "1")
                    RadioFemale.IsChecked = true;
                else
                    RadioMale.IsChecked = true;
            }

            StartAvatarAnimation();
        }

        /// <summary>
        /// Copilot 头像逐帧动画：等价于 CSS 里移动 background-position 播放 sprite 长图。
        /// 80x80 裁剪 Canvas 内，整张长图每帧上移 80px（一帧的高度），循环 54 帧。
        /// 用 Canvas.Top 布局定位（而非 RenderTransform），确保 ClipToBounds 正确裁剪。
        /// </summary>
        private void StartAvatarAnimation()
        {
            if (_avatarTimer != null)
                return;

            _avatarTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(AvatarFrameIntervalMs)
            };
            _avatarTimer.Tick += (s, e) =>
            {
                _avatarFrame = (_avatarFrame + 1) % AvatarFrameCount;
                Canvas.SetTop(AvatarImage, -_avatarFrame * AvatarFrameDisplaySize);
            };
            _avatarTimer.Start();
        }

        private void RadioMale_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is PageUserCenterViewModel vm)
            {
                vm.Sex = "0";
            }
        }

        private void RadioFemale_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is PageUserCenterViewModel vm)
            {
                vm.Sex = "1";
            }
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PageUserCenterViewModel vm)
            {
                vm.OldPassword = OldPasswordBox.Password;
                vm.NewPassword = NewPasswordBox.Password;
                vm.ConfirmPassword = ConfirmPasswordBox.Password;
                vm.ChangePasswordCommand.Execute(null);

                OldPasswordBox.Password = string.Empty;
                NewPasswordBox.Password = string.Empty;
                ConfirmPasswordBox.Password = string.Empty;
            }
        }
    }
}
