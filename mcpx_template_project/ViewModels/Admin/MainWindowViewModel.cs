using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using iNKORE.UI.WPF.Modern.Controls;
using mcpx_template_project.Core.Logging;
using mcpx_template_project.Domain.Entities.Business.Trigger;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.Machine.impl;
using mcpx_template_project.Service.Business.Plc;
using mcpx_template_project.Service.Business.Secs.impl;
using mcpx_template_project.Service.Business.Trigger;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Navigation;

namespace mcpx_template_project.ViewModels.Admin
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILogService _logService;
        private readonly IOperLogService _operLogService;
        private readonly IUserSessionService _session;
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
        private readonly IViewModelFactory _viewModelFactory;
        private readonly IPlcAutoConnectService _plcAutoConnectService;
        private readonly IPlcTriggerMonitorService _triggerMonitorService;
        private readonly ISecsGemAutoStartService _secsGemAutoStartService;
        private readonly MachinePlcPollService _machinePollService;
        private readonly INoticeService _noticeService;

        public ILogService LogService => _logService;
        public IOperLogService OperLogService => _operLogService;
        public IUserSessionService Session => _session;

        [ObservableProperty]
        private bool _isToolsPopupOpen;

        [ObservableProperty]
        private string _loginEmployeeId = "未登录";

        /// <summary>侧边栏是否展开（汉堡按钮切换）</summary>
        [ObservableProperty]
        private bool _isPaneOpen = true;

        /// <summary>侧边栏搜索关键字</summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>当前正在发生的警报数量（驱动侧边栏警报数角标）</summary>
        [ObservableProperty]
        private int _activeAlarmCount;

        /// <summary>当前用户未读通知公告数量（驱动侧边栏通知公告角标）</summary>
        [ObservableProperty]
        private int _unreadNoticeCount;

        /// <summary>侧边栏菜单树（仅结构变更时重建；搜索过滤由 View 通过菜单项可见性完成）</summary>
        public ObservableCollection<SysMenuVO> FilteredMenus { get; } = new();

        /// <summary>
        /// 菜单结构版本号：登录/登出/菜单变更/清空搜索时自增。
        /// View 据此区分「结构变更（需重建 NavigationView 菜单项）」与「仅搜索过滤（只切可见性）」，
        /// 避免每次按键都 Clear MenuItems 触发 NavigationView 内部越界异常。
        /// </summary>
        public int MenuStructureVersion { get; private set; }

        public ObservableCollection<TabItemModel> Tabs { get; } = new();

        [ObservableProperty]
        private TabItemModel? _selectedTab;

        private const string HomePageType = "mcpx_template_project.Views.Pages.Business.PageHome";
        private const string UserCenterPageType = "mcpx_template_project.Views.Pages.Admin.UserCenter.PageUserCenter";

        public event Action? RequestShowSerialConfig;
        public event Action? RequestShowLogin;
        public event Action? RequestShowPlcMonitor;
        public event Action? RequestShowSecsGem;

        /// <summary>菜单树重建完成（登录/登出/菜单变更/搜索过滤后）时触发，供 View 重建 NavigationView 菜单项。</summary>
        public event Action? MenusRebuilt;

        public MainWindowViewModel(
            ILogService logService,
            IOperLogService operLogService,
            IUserSessionService session,
            IAuthService authService,
            INavigationService navigationService,
            IDialogService dialogService,
            IViewModelFactory viewModelFactory,
            IPlcAutoConnectService plcAutoConnectService,
            IPlcTriggerMonitorService triggerMonitorService,
            ISecsGemAutoStartService secsGemAutoStartService,
            MachinePlcPollService machinePollService,
            INoticeService noticeService)
        {
            _logService = logService;
            _operLogService = operLogService;
            _session = session;
            _authService = authService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _viewModelFactory = viewModelFactory;
            _plcAutoConnectService = plcAutoConnectService;
            _triggerMonitorService = triggerMonitorService;
            _secsGemAutoStartService = secsGemAutoStartService;
            _machinePollService = machinePollService;
            _noticeService = noticeService;

            InitializeHomeTab();

            // 订阅警报事件：上升沿弹窗、活跃警报集合变化刷新角标
            _triggerMonitorService.AlarmRaised += OnAlarmRaised;
            _triggerMonitorService.ActiveAlarmsChanged += OnActiveAlarmsChanged;

            WeakReferenceMessenger.Default.Register<LoginSuccessMessage>(this, (recipient, message) =>
            {
                LoginEmployeeId = message.User.UserName;
                RebuildFilteredMenus();
                NavigateToFirstMenu();
                // 登录成功后自动连接所有 auto 模式的通讯设定
                _ = AutoConnectPlcAsync();
                // 登录后若 SECS/GEM 为 auto 模式，自动开放监听
                _ = AutoStartSecsGemAsync();
                // 启动警报后台监控
                _triggerMonitorService.Start();
                // 启动机台 PLC 后台轮询（首页机台监控数据源）
                _machinePollService.Start();
                // 刷新未读通知角标
                _ = RefreshUnreadNoticeCountAsync();
            });

            WeakReferenceMessenger.Default.Register<LogoutMessage>(this, (recipient, message) =>
            {
                LoginEmployeeId = "未登录";
                ResetToHome();
                // 登出停止警报监控并清零角标
                _triggerMonitorService.Stop();
                ActiveAlarmCount = 0;
                UnreadNoticeCount = 0;
            });

            WeakReferenceMessenger.Default.Register<MenuChangedMessage>(this, async (recipient, message) =>
            {
                await RefreshSessionMenusAsync();
                RebuildFilteredMenus();
            });

            WeakReferenceMessenger.Default.Register<NoticeChangedMessage>(this, (recipient, message) =>
            {
                _ = RefreshUnreadNoticeCountAsync();
            });

            // 启动时若已登录（从登录窗口进入），直接初始化用户信息
            if (_session.IsLoggedIn && _session.CurrentUser != null)
            {
                LoginEmployeeId = _session.CurrentUser.UserName;
                RebuildFilteredMenus();
                NavigateToFirstMenu();
                // 已登录直接自动连接所有 auto 模式的通讯设定
                _ = AutoConnectPlcAsync();
                // 已登录且 SECS/GEM 为 auto 模式，自动开放监听
                _ = AutoStartSecsGemAsync();
                // 启动警报后台监控
                _triggerMonitorService.Start();
                // 启动机台 PLC 后台轮询（首页机台监控数据源）
                _machinePollService.Start();
                // 刷新未读通知角标
                _ = RefreshUnreadNoticeCountAsync();
            }
        }

        /// <summary>警报点位触发：弹窗提示并响铃。</summary>
        private void OnAlarmRaised(PlcTrigger trigger)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            app.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    System.Media.SystemSounds.Beep.Play();
                    //System.Windows.MessageBox.Show(
                    //    trigger.Remark,
                    //    trigger.Name,
                    //    System.Windows.MessageBoxButton.OK,
                    //    System.Windows.MessageBoxImage.Warning,
                    //    System.Windows.MessageBoxResult.OK);
                    MessageBox.Show(trigger.Remark, trigger.Name, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
                catch
                {
                    // 弹窗失败不阻断
                }
            });
        }

        /// <summary>活跃警报集合变化：刷新侧边栏警报数角标。</summary>
        private void OnActiveAlarmsChanged()
        {
            ActiveAlarmCount = _triggerMonitorService.ActiveAlarmCount;
        }

        /// <summary>
        /// 刷新未读通知公告角标。供登录后、以及 S10F3 写入新通知时调用（由通知写入方通过消息触发，
        /// 这里也提供公共入口供 PageNotice 标记已读后回调）。
        /// </summary>
        public async Task RefreshUnreadNoticeCountAsync()
        {
            try
            {
                if (!_session.IsLoggedIn)
                {
                    UnreadNoticeCount = 0;
                    return;
                }
                UnreadNoticeCount = await _noticeService.QueryUnreadCountAsync();
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "[通知] 刷新未读通知数异常");
            }
        }

        /// <summary>
        /// 登录/启动后自动连接所有「启动连线模式 = auto」的通讯设定。
        /// 静默执行，失败不弹窗打断用户，仅记录日志。
        /// </summary>
        private async Task AutoConnectPlcAsync()
        {
            try
            {
                await _plcAutoConnectService.AutoConnectAllAsync();
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "[PLC] 自动连线异常");
            }
        }

        /// <summary>
        /// 登录/启动后若 SECS/GEM 为 auto 模式，自动开放 Passive 监听。
        /// 静默执行，失败不弹窗打断用户，仅记录日志。
        /// </summary>
        private async Task AutoStartSecsGemAsync()
        {
            try
            {
                await _secsGemAutoStartService.AutoStartIfNeededAsync();
            }
            catch (Exception ex)
            {
                _logService.LogError(ex, "[SECS/GEM] 自动开放监听异常");
            }
        }

        /// <summary>
        /// 搜索关键字变化：只通知 View 按关键字切换菜单项可见性。
        /// 不能在这里重建菜单结构——该回调运行在绑定「目标→源」回写链路内，
        /// 期间 MenuItems.Clear() 会让 NavigationView 内部按下标取已移除的项而抛 ArgumentOutOfRangeException，
        /// 异常被绑定吞成「Cannot save value from target back to source」，表现为搜索静默失效。
        /// </summary>
        partial void OnSearchTextChanged(string value) => MenusRebuilt?.Invoke();

        /// <summary>
        /// 重建侧边栏菜单树（仅登录/登出/菜单变更时调用，属结构变更）。
        /// 过滤后的菜单树只存完整结构，搜索过滤由 View 通过可见性完成。
        /// </summary>
        public void RebuildFilteredMenus()
        {
            FilteredMenus.Clear();
            foreach (var node in FilterMenuNodes(_session.Menus))
                FilteredMenus.Add(node);

            MenuStructureVersion++;
            MenusRebuilt?.Invoke();
        }

        private static List<SysMenuVO> FilterMenuNodes(IEnumerable<SysMenuVO> source)
        {
            var result = new List<SysMenuVO>();
            foreach (var menu in source)
            {
                if (menu.MenuType == "F") continue; // 按钮权限不进入导航
                result.Add(menu);
            }
            return result;
        }

        private Frame CreateFrameWithPage(string componentType)
        {
            var page = _navigationService.CreatePage(componentType);
            return new Frame
            {
                Content = page,
                NavigationUIVisibility = NavigationUIVisibility.Hidden
            };
        }

        private void InitializeHomeTab()
        {
            var homeTab = new TabItemModel
            {
                Title = "首页",
                Component = HomePageType,
                Frame = CreateFrameWithPage(HomePageType),
                CanClose = false
            };
            Tabs.Add(homeTab);
            SelectedTab = homeTab;
        }

        /// <summary>
        /// 判断菜单组件是否为首页：除精确匹配外，兼容数据库中历史遗留的
        /// 「...Business.Home.PageHome」等旧路径（按类型名末段识别），避免重复开出可关闭的首页页签。
        /// </summary>
        private static bool IsHomeComponent(string? component)
        {
            if (string.IsNullOrWhiteSpace(component)) return false;
            return component.Trim().Equals(HomePageType, StringComparison.OrdinalIgnoreCase)
                || component.Trim().EndsWith(".PageHome", StringComparison.OrdinalIgnoreCase);
        }

        private void ResetToHome()
        {
            Tabs.Clear();
            InitializeHomeTab();
            RebuildFilteredMenus();
        }

        private SysMenuVO? FindFirstMenuNode(IEnumerable<SysMenuVO> menus)
        {
            foreach (var menu in menus)
            {
                if (menu.MenuType == "C") return menu;
                if (menu.Children.Count > 0)
                {
                    var child = FindFirstMenuNode(menu.Children);
                    if (child != null) return child;
                }
            }
            return null;
        }

        /// <summary>
        /// 菜单管理数据变更后，重新加载当前用户菜单并更新会话，侧边栏随之自动刷新。
        /// </summary>
        private async Task RefreshSessionMenusAsync()
        {
            if (!_session.IsLoggedIn || _session.CurrentUser == null) return;

            try
            {
                var menus = await _authService.GetMenuTreeAsync(_session.CurrentUser.UserId);
                _session.UpdateMenus(menus);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"刷新菜单失败：{ex.Message}");
            }
        }

        private void NavigateToFirstMenu()
        {
            var firstMenu = FindFirstMenuNode(_session.Menus);
            if (firstMenu != null)
            {
                NavigateToMenu(firstMenu);
            }
        }

        [RelayCommand]
        private void NavigateToMenu(SysMenuVO menu)
        {
            if (menu == null || menu.MenuType != "C") return;
            if (string.IsNullOrWhiteSpace(menu.Component)) return;

            // 菜单中的「首页/主页」复用自带首页 Tab（InitializeHomeTab 创建的不可关闭 Tab），避免重复打开
            if (IsHomeComponent(menu.Component))
            {
                SelectedTab = Tabs.FirstOrDefault(t => !t.CanClose) ?? Tabs.FirstOrDefault();
                return;
            }

            var existing = Tabs.FirstOrDefault(t => t.Component == menu.Component);
            if (existing != null)
            {
                SelectedTab = existing;
                return;
            }

            var page = _navigationService.CreatePage(menu.Component);
            if (page == null)
            {
                // 组件类型无法解析（如数据库 component 配错）：提示而不是开一个空白页签
                _dialogService.ShowError($"无法打开「{menu.MenuName}」：未找到页面类型 {menu.Component}");
                return;
            }

            var tab = new TabItemModel
            {
                Title = menu.MenuName,
                Component = menu.Component,
                Frame = new Frame
                {
                    Content = page,
                    NavigationUIVisibility = NavigationUIVisibility.Hidden
                },
                CanClose = true
            };
            Tabs.Add(tab);
            SelectedTab = tab;
        }

        [RelayCommand]
        private void NavigateToUserCenter()
        {
            var existing = Tabs.FirstOrDefault(t => t.Component == UserCenterPageType);
            if (existing != null)
            {
                SelectedTab = existing;
                return;
            }

            var tab = new TabItemModel
            {
                Title = "个人中心",
                Component = UserCenterPageType,
                Frame = CreateFrameWithPage(UserCenterPageType),
                CanClose = true
            };
            Tabs.Add(tab);
            SelectedTab = tab;
        }

        [RelayCommand]
        private void CloseTab(TabItemModel? tab)
        {
            if (tab == null || !tab.CanClose) return;

            int index = Tabs.IndexOf(tab);
            if (index < 0) return;

            bool isSelected = SelectedTab == tab;
            Tabs.RemoveAt(index);

            if (isSelected && Tabs.Count > 0)
            {
                int newIndex = Math.Min(index, Tabs.Count - 1);
                SelectedTab = Tabs[newIndex];
            }
        }

        [RelayCommand]
        private void CloseOtherTabs()
        {
            if (SelectedTab == null || !SelectedTab.CanClose) return;
            var tabsToRemove = Tabs.Where(t => t != SelectedTab && t.CanClose).ToList();
            foreach (var tab in tabsToRemove)
                Tabs.Remove(tab);
        }

        [RelayCommand]
        private void CloseAllTabs()
        {
            var tabsToRemove = Tabs.Where(t => t.CanClose).ToList();
            foreach (var tab in tabsToRemove)
                Tabs.Remove(tab);
            if (Tabs.Count > 0)
                SelectedTab = Tabs[0];
        }

        [RelayCommand]
        private void CloseLeftTabs()
        {
            if (SelectedTab == null || !SelectedTab.CanClose) return;
            int index = Tabs.IndexOf(SelectedTab);
            if (index <= 0) return;
            var tabsToRemove = Tabs.Take(index).Where(t => t.CanClose).ToList();
            foreach (var tab in tabsToRemove)
                Tabs.Remove(tab);
        }

        [RelayCommand]
        private void CloseRightTabs()
        {
            if (SelectedTab == null || !SelectedTab.CanClose) return;
            int index = Tabs.IndexOf(SelectedTab);
            if (index < 0) return;
            var tabsToRemove = Tabs.Skip(index + 1).Where(t => t.CanClose).ToList();
            foreach (var tab in tabsToRemove)
                Tabs.Remove(tab);
        }

        [RelayCommand]
        private void ShowLogin()
        {
            RequestShowLogin?.Invoke();
        }

        [RelayCommand]
        private void ShowSerialConfig()
        {
            RequestShowSerialConfig?.Invoke();
        }

        [RelayCommand]
        private void ShowPlcMonitor()
        {
            RequestShowPlcMonitor?.Invoke();
        }

        [RelayCommand]
        private void ShowSecsGem()
        {
            RequestShowSecsGem?.Invoke();
        }

        [RelayCommand]
        private void ShowToolsPopup()
        {
            IsToolsPopupOpen = true;
        }

        [RelayCommand]
        private void Logout()
        {
            var result = _dialogService.Confirm("确定要退出当前登录的用户吗？", "确认退出登录");
            if (!result) return;

            _session.Logout();
            _navigationService.ClearCache();
            WeakReferenceMessenger.Default.Send(new LogoutMessage());

            RequestShowLogin?.Invoke();
        }

        [RelayCommand]
        private void ToolsOpen(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;

            string? exePath = tag switch
            {
                "Poll" => @"D:\Workspace\modbus\Modbus Poll\mbpoll.exe",
                "Slave" => @"D:\Workspace\modbus\Modbus Slave\mbslave.exe",
                "VSPD" => @"D:\Workspace\modbus\VSPD.exe",
                "GitHub" => @"https://github.com/",
                _ => null
            };

            if (string.IsNullOrEmpty(exePath)) return;
            try
            {
                if (exePath.StartsWith("http://") || exePath.StartsWith("https://"))
                    Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
                else
                    Process.Start(exePath);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"启动失败：{ex.Message}");
            }
        }
    }
}
