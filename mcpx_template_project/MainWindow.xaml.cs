using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.ViewModels.Admin;
using mcpx_template_project.Views.Pages.Admin;
using mcpx_template_project.Views.Pages.Business;
using mcpx_template_project.Views.Pages.Business.Monitor;
using mcpx_template_project.Views.Pages.Business.SecsGem;
using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Controls;
using mcpx_template_project.Helper;

namespace mcpx_template_project
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel? _mainWindowViewModel;
        private readonly LoginViewModel? _loginViewModel;

        private Login? _login;

        /// <summary>通讯设定窗口（单例，关闭仅隐藏，生命周期不消亡）</summary>
        private CommSettingWindow? _commSettingWindow;

        /// <summary>实时监控窗口（单例，关闭仅隐藏，生命周期不消亡）</summary>
        private PlcMonitorWindow? _plcMonitorWindow;

        /// <summary>SECS/GEM 通讯设定窗口（单例，关闭仅隐藏，生命周期不消亡）</summary>
        private SecsGemSettingWindow? _secsGemSettingWindow;

        public MainWindow(MainWindowViewModel mainWindowViewModel, LoginViewModel loginViewModel)
        {
            InitializeComponent();
            _mainWindowViewModel = mainWindowViewModel;
            _loginViewModel = loginViewModel;
            this.DataContext = _mainWindowViewModel;

            if (_mainWindowViewModel != null)
            {
                _mainWindowViewModel.RequestShowLogin += OnRequestShowLogin;
                _mainWindowViewModel.RequestShowSerialConfig += OnRequestShowSerialConfig;
                _mainWindowViewModel.RequestShowPlcMonitor += OnRequestShowPlcMonitor;
                _mainWindowViewModel.RequestShowSecsGem += OnRequestShowSecsGem;
                _mainWindowViewModel.MenusRebuilt += RefreshNavigation;
            }

            // 首次构建侧边栏菜单（若已登录则填充，否则等登录消息触发重建）
            RefreshNavigation();

            // 订阅窗口状态变化，同步最大化/还原图标
            StateChanged += (_, _) => UpdateMaximizeIcon();

            // 修复 WindowStyle=None 时最大化覆盖任务栏的问题
            SourceInitialized += (_, _) =>
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var source = System.Windows.Interop.HwndSource.FromHwnd(hwnd);
                source?.AddHook(WndProc);
                // 让 DWM 背景材质跟随应用明暗主题（否则深色系统下亚克力/云母发白）
                DwmWindowHelper.ApplyWindowBackdrop(this);
            };
        }

        private const int WM_GETMINMAXINFO = 0x0024;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO))!;
                // 用当前屏幕的工作区（排除任务栏）作为最大化边界
                var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                    if (GetMonitorInfo(monitor, ref info))
                    {
                        mmi.ptMaxPosition.X = info.rcWork.Left;
                        mmi.ptMaxPosition.Y = info.rcWork.Top;
                        mmi.ptMaxSize.X = info.rcWork.Right - info.rcWork.Left;
                        mmi.ptMaxSize.Y = info.rcWork.Bottom - info.rcWork.Top;
                    }
                }
                Marshal.StructureToPtr(mmi, lParam, true);
                handled = true;
            }
            return IntPtr.Zero;
        }

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 双击标题栏切换最大化/还原
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }

            // 单击拖动窗口（最大化状态下不拖动，先还原）
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                if (WindowState == WindowState.Maximized)
                {
                    ToggleMaximize();
                }
                else
                {
                    DragMove();
                }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
            UpdateMaximizeIcon();
        }

        private void UpdateMaximizeIcon()
        {
            if (MaximizeIcon == null) return;
            MaximizeIcon.Icon = WindowState == WindowState.Maximized
                ? SegoeFluentIcons.ChromeRestore
                : SegoeFluentIcons.ChromeMaximize;
        }

        private void OnRequestShowLogin()
        {
            if (_loginViewModel != null)
            {
                _login = new Login(_loginViewModel);
                _login.Owner = this;
                _login.ShowDialog();
            }
        }

        /// <summary>
        /// 打开通讯设定窗口：单例复用，窗口关闭仅隐藏，再次点击 IOT 图标时恢复显示。
        /// </summary>
        private void OnRequestShowSerialConfig()
        {
            if (_commSettingWindow == null)
            {
                _commSettingWindow = ((App)Application.Current).Services!
                    .GetRequiredService<CommSettingWindow>();
                _commSettingWindow.Owner = this;
            }

            if (_commSettingWindow.IsVisible)
                _commSettingWindow.Activate();
            else
                _commSettingWindow.Show();
        }

        /// <summary>
        /// 打开实时监控窗口：单例复用，窗口关闭仅隐藏。
        /// </summary>
        private void OnRequestShowPlcMonitor()
        {
            if (_plcMonitorWindow == null)
            {
                _plcMonitorWindow = ((App)Application.Current).Services!
                    .GetRequiredService<PlcMonitorWindow>();
                _plcMonitorWindow.Owner = this;
            }

            if (_plcMonitorWindow.IsVisible)
                _plcMonitorWindow.Activate();
            else
                _plcMonitorWindow.Show();
        }

        /// <summary>
        /// 打开 SECS/GEM 通讯设定窗口：单例复用，关闭仅隐藏。
        /// </summary>
        private void OnRequestShowSecsGem()
        {
            if (_secsGemSettingWindow == null)
            {
                _secsGemSettingWindow = ((App)Application.Current).Services!
                    .GetRequiredService<SecsGemSettingWindow>();
                _secsGemSettingWindow.Owner = this;
            }

            if (_secsGemSettingWindow.IsVisible)
                _secsGemSettingWindow.Activate();
            else
                _secsGemSettingWindow.Show();
        }

        /// <summary>
        /// 菜单刷新入口：仅在「菜单结构版本」变化时重建 NavigationView 菜单项，
        /// 搜索关键字变化只切换菜单项可见性。
        ///
        /// 不能在搜索时重建：iNKORE NavigationView 在 MenuItems.Clear() 且存在选中项时内部会抛
        /// ArgumentOutOfRangeException；该异常在绑定回写链路里被吞成「Cannot save value from target
        /// back to source」，表现为输入框内容不再回写 ViewModel、搜索静默失效。
        /// </summary>
        private void RefreshNavigation()
        {
            if (_mainWindowViewModel == null || NavigationViewRoot == null) return;

            if (_builtMenuStructureVersion != _mainWindowViewModel.MenuStructureVersion)
            {
                RebuildNavigationMenu();
                _builtMenuStructureVersion = _mainWindowViewModel.MenuStructureVersion;
            }

            ApplyMenuFilter(_mainWindowViewModel.SearchText);
        }

        /// <summary>已构建菜单项对应的菜单结构版本，用于判断是否需要重建。</summary>
        private int _builtMenuStructureVersion = -1;

        /// <summary>
        /// 重建 NavigationView 菜单项（只在登录/登出/菜单变更/清空搜索时执行）。
        /// 递归生成 NavigationViewItem：目录（M）作为带子菜单的父项，菜单（C）作为可点击叶子项。
        /// </summary>
        private void RebuildNavigationMenu()
        {
            if (_mainWindowViewModel == null || NavigationViewRoot == null) return;

            // 先清空选中项再清空集合：存在选中项时 Clear() 会让 NavigationView 内部按下标取选中项而越界
            NavigationViewRoot.SelectedItem = null;
            NavigationViewRoot.MenuItems.Clear();

            foreach (var menu in _mainWindowViewModel.FilteredMenus)
            {
                var item = BuildNavigationItem(menu);
                if (item != null)
                    NavigationViewRoot.MenuItems.Add(item);
            }
        }

        /// <summary>
        /// 按关键字切换菜单项可见性（不改变集合，避免触发 NavigationView 的选择逻辑）。
        /// 命中节点及其父级链保持可见并自动展开；父级名称命中时整棵子树保持可见。
        /// </summary>
        private void ApplyMenuFilter(string? keyword)
        {
            if (NavigationViewRoot == null) return;

            bool filtering = !string.IsNullOrWhiteSpace(keyword);
            foreach (var item in NavigationViewRoot.MenuItems.OfType<NavigationViewItem>())
                ApplyMenuItemFilter(item, keyword, filtering);
        }

        private static bool ApplyMenuItemFilter(NavigationViewItem item, string? keyword, bool filtering)
        {
            // 父级已命中 → 整棵子树视为命中
            bool selfMatch = !filtering
                || (item.Tag is SysMenuVO menu
                    && (menu.MenuName?.Contains(keyword!, StringComparison.OrdinalIgnoreCase) ?? false));

            bool childVisible = false;
            foreach (var child in item.MenuItems.OfType<NavigationViewItem>())
            {
                if (ApplyMenuItemFilter(child, keyword, filtering))
                    childVisible = true;
            }

            bool visible = selfMatch || childVisible;
            item.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            // 搜索时展开命中节点的父级链，否则命中二级/三级菜单仍要手动逐级展开
            if (filtering && visible && item.MenuItems.Count > 0)
                item.IsExpanded = true;

            return visible;
        }

        /// <summary>递归构建单个 NavigationViewItem，Icon 由 MenuIconConverter 从 sys_menu.icon 动态解析。</summary>
        private NavigationViewItem? BuildNavigationItem(SysMenuVO menu)
        {
            if (menu == null) return null;

            var item = new NavigationViewItem
            {
                Content = menu.MenuName,
                Tag = menu,
                // 目录（M）点击不导航，只展开/收起；叶子菜单（C）点击触发导航
                SelectsOnInvoked = menu.MenuType == "C"
            };

            // 图标：复用 MenuIconConverter 的解析逻辑（官方 SegoeFluentIcons / 语义别名 / 十六进制字形码 / 关键字兜底）
            if (MenuIconConverter.ResolveIcon(menu.Icon, menu.MenuName, menu.MenuType) is FontIconData iconData)
            {
                item.Icon = new FontIcon(iconData) { FontSize = 16 };
            }

            // 警报菜单项：附加警报数角标（InfoBadge），绑定当前正在发生的警报数量
            if (menu.MenuType == "C" && menu.Component == AlarmComponentType)
            {
                item.InfoBadge = BuildAlarmInfoBadge();
            }

            // 通知公告菜单项：附加未读角标，绑定当前用户未读通知数量
            if (menu.MenuType == "C" && menu.Component == NoticeComponentType)
            {
                item.InfoBadge = BuildNoticeInfoBadge();
            }

            // 递归子菜单
            if (menu.Children is { Count: > 0 })
            {
                foreach (var child in menu.Children)
                {
                    var childItem = BuildNavigationItem(child);
                    if (childItem != null)
                        item.MenuItems.Add(childItem);
                }
            }

            return item;
        }

        /// <summary>警报菜单组件类型全名（与 DatabaseInitializer 建菜单一致）</summary>
        private const string AlarmComponentType = "mcpx_template_project.Views.Pages.Business.Trigger.PageAlarm";

        /// <summary>通知公告菜单组件类型全名（与 sys_menu 种子数据 menu_id=14 一致）</summary>
        private const string NoticeComponentType = "mcpx_template_project.Views.Pages.Admin.Notice.PageNotice";

        /// <summary>构建警报数 InfoBadge，值随 MainWindowViewModel.ActiveAlarmCount 实时刷新，为 0 时隐藏。</summary>
        private InfoBadge BuildAlarmInfoBadge()
        {
            var badge = new InfoBadge
            {
                Opacity = 1,
                Value = 0
            };

            // 绑定 Value（警报数）
            var valueBinding = new System.Windows.Data.Binding(nameof(MainWindowViewModel.ActiveAlarmCount))
            {
                Source = _mainWindowViewModel
            };
            System.Windows.Data.BindingOperations.SetBinding(badge, InfoBadge.ValueProperty, valueBinding);

            // 警报数为 0 时隐藏角标
            var visibilityBinding = new System.Windows.Data.Binding(nameof(MainWindowViewModel.ActiveAlarmCount))
            {
                Source = _mainWindowViewModel,
                Converter = new Converters.IntToVisibilityConverter()
            };
            System.Windows.Data.BindingOperations.SetBinding(badge, System.Windows.UIElement.VisibilityProperty, visibilityBinding);

            return badge;
        }

        /// <summary>构建通知未读数 InfoBadge，值随 MainWindowViewModel.UnreadNoticeCount 实时刷新，为 0 时隐藏。</summary>
        private InfoBadge BuildNoticeInfoBadge()
        {
            var badge = new InfoBadge
            {
                Opacity = 1,
                Value = 0
            };

            // 绑定 Value（未读通知数）
            var valueBinding = new System.Windows.Data.Binding(nameof(MainWindowViewModel.UnreadNoticeCount))
            {
                Source = _mainWindowViewModel
            };
            System.Windows.Data.BindingOperations.SetBinding(badge, InfoBadge.ValueProperty, valueBinding);

            // 未读数为 0 时隐藏角标
            var visibilityBinding = new System.Windows.Data.Binding(nameof(MainWindowViewModel.UnreadNoticeCount))
            {
                Source = _mainWindowViewModel,
                Converter = new Converters.IntToVisibilityConverter()
            };
            System.Windows.Data.BindingOperations.SetBinding(badge, System.Windows.UIElement.VisibilityProperty, visibilityBinding);

            return badge;
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (_mainWindowViewModel == null) return;

            // 侧边栏底部（FooterMenuItems）的个人中心 / 退出登录
            if (args.InvokedItemContainer is NavigationViewItem navItem && navItem.Tag is string tag)
            {
                switch (tag)
                {
                    case "UserCenter":
                        _mainWindowViewModel.NavigateToUserCenterCommand.Execute(null);
                        break;
                    case "Logout":
                        _mainWindowViewModel.LogoutCommand.Execute(null);
                        break;
                }
                return;
            }

            // 选中的菜单项容器（含 Tag=SysMenuVO）
            if (args.InvokedItemContainer is NavigationViewItem menuItem && menuItem.Tag is SysMenuVO menu)
            {
                // 只对叶子菜单（C）导航；目录（M）不响应
                if (menu.MenuType == "C")
                {
                    _mainWindowViewModel.NavigateToMenuCommand.Execute(menu);
                }
            }
        }

        private static T? FindAncestor<T>(DependencyObject element) where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T result) return result;
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            }
            return null;
        }

        private void ToolsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_mainWindowViewModel != null)
            {
                _mainWindowViewModel.IsToolsPopupOpen = !_mainWindowViewModel.IsToolsPopupOpen;
            }
        }

        private void TabItem_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_mainWindowViewModel == null) return;

            var border = sender as Border;
            if (border == null) return;

            var tabItem = FindAncestor<TabItem>(border);
            if (tabItem == null) return;

            // 选中右键的标签页
            tabItem.IsSelected = true;

            // 首页不可关闭，不显示右键菜单
            if (tabItem.DataContext is TabItemModel tab && !tab.CanClose)
            {
                e.Handled = true;
            }
        }

        private void ContextMenu_CloseOtherTabs(object sender, RoutedEventArgs e)
        {
            _mainWindowViewModel?.CloseOtherTabsCommand.Execute(null);
        }

        private void ContextMenu_CloseAllTabs(object sender, RoutedEventArgs e)
        {
            _mainWindowViewModel?.CloseAllTabsCommand.Execute(null);
        }

        private void ContextMenu_CloseLeftTabs(object sender, RoutedEventArgs e)
        {
            _mainWindowViewModel?.CloseLeftTabsCommand.Execute(null);
        }

        private void ContextMenu_CloseRightTabs(object sender, RoutedEventArgs e)
        {
            _mainWindowViewModel?.CloseRightTabsCommand.Execute(null);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }
    }
}
