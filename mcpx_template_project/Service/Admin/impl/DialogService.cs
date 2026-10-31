using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading.Tasks;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Common.IconKeys;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.ViewModels.Admin.Dept;
using mcpx_template_project.ViewModels.Admin.Menu;
using mcpx_template_project.ViewModels.Admin.Notice;
using mcpx_template_project.ViewModels.Admin.Post;
using mcpx_template_project.ViewModels.Admin.Role;
using mcpx_template_project.ViewModels.Admin.User;
using mcpx_template_project.Views.Pages.Admin.Dept;
using mcpx_template_project.Views.Pages.Admin.Menu;
using mcpx_template_project.Views.Pages.Admin.Notice;
using mcpx_template_project.Views.Pages.Admin.OperLog;
using mcpx_template_project.Views.Pages.Admin.Post;
using mcpx_template_project.Views.Pages.Admin.Role;
using mcpx_template_project.Views.Pages.Admin.User;
using mcpx_template_project.Views.Controls;

namespace mcpx_template_project.Service.Admin.impl
{
    /// <summary>
    /// 基于 iNKORE（Fluent 2）ContentDialog 的对话框服务实现。
    /// 提示/确认类对话框改用 Fluent ContentDialog（带 FontIcon 图标区分类型）；
    /// 复杂表单窗口仍通过 <see cref="WindowMap"/> 注册表统一创建。
    /// 所有调用统一调度到主线程，避免跨线程访问 UI。
    /// </summary>
    public class DialogService : IDialogService
    {
        /// <summary>视图模型/数据对象类型 → 对话框窗口工厂映射</summary>
        private static readonly Dictionary<Type, Func<object, Window>> WindowMap = new()
        {
            [typeof(AddUserViewModel)] = vm => new AddUserWindow((AddUserViewModel)vm),
            [typeof(EditUserViewModel)] = vm => new EditUserWindow((EditUserViewModel)vm),
            [typeof(AddDeptViewModel)] = vm => new AddDeptWindow((AddDeptViewModel)vm),
            [typeof(AddEditPostViewModel)] = vm => new AddEditPostWindow((AddEditPostViewModel)vm),
            [typeof(AddEditMenuViewModel)] = vm => new AddEditMenuWindow((AddEditMenuViewModel)vm),
            [typeof(AddEditRoleViewModel)] = vm => new AddEditRoleWindow((AddEditRoleViewModel)vm),
            [typeof(AddEditNoticeViewModel)] = vm => new AddEditNoticeWindow((AddEditNoticeViewModel)vm),
            [typeof(NoticeDetailViewModel)] = vm => new NoticeDetailWindow((NoticeDetailViewModel)vm),
            [typeof(SysOperLogVO)] = vm => new OperLogDetailWindow((SysOperLogVO)vm),
        };

        public void ShowInfo(string message, string title = "提示")
        {
            Dispatch(() => ShowContentDialog(title, message, "知道了", null, null, SegoeFluentIcons.Info, "App.Accent"));
        }

        public void ShowSuccess(string message, string title = "成功")
        {
            Dispatch(() => ShowContentDialog(title, message, "知道了", null, null, SegoeFluentIcons.CheckMark, "App.Success"));
        }

        public void ShowError(string message, string title = "错误")
        {
            Dispatch(() => ShowContentDialog(title, message, "知道了", null, null, SegoeFluentIcons.Error, "App.Danger"));
        }

        public void ShowWarning(string message, string title = "警告")
        {
            Dispatch(() => ShowContentDialog(title, message, "知道了", null, null, SegoeFluentIcons.Warning, "App.Warning"));
        }

        public bool Confirm(string message, string title = "确认")
        {
            return Dispatch(() => ShowContentDialog(title, message, "否", "是", null, SegoeFluentIcons.Help, "App.Accent") == ContentDialogResult.Primary);
        }

        public bool? ShowDialog<TViewModel>(TViewModel viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            return Dispatch(() =>
            {
                var vmType = viewModel.GetType();
                if (!WindowMap.TryGetValue(vmType, out var factory))
                    throw new ArgumentException($"未注册的对话框视图模型类型: {vmType.Name}", nameof(viewModel));

                var window = factory(viewModel);
                window.Owner = FindOwner();
                return window.ShowDialog();
            });
        }

        /// <summary>
        /// 用 Fluent ContentDialog 同步显示一个提示/确认对话框（带可选 FontIcon 图标）。
        /// ContentDialog.ShowAsync 是异步非阻塞（Popup + Adorner），不能用 GetResult 同步等待（会死锁），
        /// 这里用 DispatcherFrame 泵消息循环，在对话框关闭前保持 UI 响应。
        /// </summary>
        private static ContentDialogResult ShowContentDialog(
            string title, string content, string? closeText, string? primaryText, string? secondaryText,
            FontIconData? iconData, string? iconBrushKey)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = BuildContent(content, iconData, iconBrushKey)
            };
            if (closeText != null) dialog.CloseButtonText = closeText;
            if (primaryText != null) dialog.PrimaryButtonText = primaryText;
            if (secondaryText != null) dialog.SecondaryButtonText = secondaryText;
            dialog.DefaultButton = primaryText != null ? ContentDialogButton.Primary : ContentDialogButton.Close;

            var owner = FindOwner();
            var showTask = owner != null ? dialog.ShowAsync(owner) : dialog.ShowAsync();

            ContentDialogResult result = ContentDialogResult.None;
            var frame = new DispatcherFrame();
            showTask.ContinueWith(t =>
            {
                try { result = t.Result; }
                catch { result = ContentDialogResult.None; }
                frame.Continue = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.PushFrame(frame);
            return result;
        }

        /// <summary>构建对话框内容：图标 + 文字（图标列固定，文字列自适应换行）</summary>
        private static object BuildContent(string content, FontIconData? iconData, string? iconBrushKey)
        {
            if (iconData == null)
                return content;

            var icon = new FontIcon
            {
                Icon = iconData,
                FontSize = 28,
                Foreground = ResolveBrush(iconBrushKey),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 3, 0, 0)
            };

            var text = new TextBlock
            {
                Text = content,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.Children.Add(icon);
            Grid.SetColumn(icon, 0);

            grid.Children.Add(text);
            Grid.SetColumn(text, 2);

            return grid;
        }

        private static Brush? ResolveBrush(string? key)
            => key != null ? Application.Current?.TryFindResource(key) as Brush : null;

        /// <summary>
        /// 查找当前可见窗口作为对话框宿主。注意 Application.MainWindow 默认指向第一个实例化的窗口
        /// （即已关闭的登录窗），不能直接依赖它。
        /// </summary>
        private static Window? FindOwner()
        {
            var app = Application.Current;
            if (app == null)
                return null;

            // 优先返回当前拥有焦点/激活的窗口（如通讯设定弹窗、编辑对话框），
            // 保证提示/确认对话框显示在用户正在操作的窗口之上，而非总是主窗口。
            return app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                ?? app.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);
        }

        private static void Dispatch(Action action)
        {
            if (Application.Current?.Dispatcher?.CheckAccess() != false)
            {
                action();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(action);
            }
        }

        private static T Dispatch<T>(Func<T> func)
        {
            if (Application.Current?.Dispatcher?.CheckAccess() != false)
            {
                return func();
            }
            return Application.Current.Dispatcher.Invoke(func);
        }
    }
}
