using System.Collections;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using mcpx_template_project.Helper;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.ViewModels.Common;

namespace mcpx_template_project.Views.Controls
{
    /// <summary>
    /// 可复用的 DataGrid 右键菜单：根据 ViewModel 提供的 <see cref="RowAction"/> 列表自动生成菜单项，
    /// 打开前按 <see cref="RowAction.PermCode"/> 做按钮级权限过滤（无权限的项不生成）。
    /// 数据选中完全由用户通过复选框（约定数据对象含 bool IsSelected 属性）完成，右键仅负责弹出菜单：
    /// 没有勾选任何数据行（或权限过滤后无可执行项）时，菜单在弹出前即被取消，不会出现空菜单闪烁。
    /// 用法：
    /// <code>
    /// &lt;DataGrid.ContextMenu&gt;
    ///     &lt;ctrl:RowContextMenu Actions="{Binding PlacementTarget.DataContext.RowActions, RelativeSource={RelativeSource Self}}"/&gt;
    /// &lt;/DataGrid.ContextMenu&gt;
    /// </code>
    /// </summary>
    public class RowContextMenu : ContextMenu
    {
        static RowContextMenu()
        {
            // 在菜单真正弹出前（DataGrid 的 ContextMenuOpening 路由事件阶段）拦截：
            // 不满足条件时直接 Handled 取消，避免空菜单先弹出再关闭的闪烁。
            EventManager.RegisterClassHandler(
                typeof(DataGrid),
                ContextMenuOpeningEvent,
                new ContextMenuEventHandler(OnDataGridContextMenuOpening));
        }

        public static readonly DependencyProperty ActionsProperty =
            DependencyProperty.Register(nameof(Actions), typeof(IEnumerable), typeof(RowContextMenu), new PropertyMetadata(null));

        /// <summary>右键菜单操作项集合（由页面 ViewModel 提供）</summary>
        public IEnumerable? Actions
        {
            get => (IEnumerable?)GetValue(ActionsProperty);
            set => SetValue(ActionsProperty, value);
        }

        public RowContextMenu()
        {
            // 样式内置在控件专属资源字典中，使用方无需任何额外配置
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/mcpx_template_project;component/Views/Controls/RowContextMenu.xaml", UriKind.Absolute)
            });
            Style = (Style)Resources["RowContextMenuStyle"]!;
        }

        private static void OnDataGridContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (sender is DataGrid grid && grid.ContextMenu is RowContextMenu menu)
                menu.HandleOpening(grid, e);
        }

        private void HandleOpening(DataGrid grid, ContextMenuEventArgs e)
        {
            // 定位右键所在行的数据（右键空白处为 null）
            var rowData = GetRowData(e);

            // 操作目标数量：勾选了复选框则按勾选行数；无勾选且右键在行上时默认操作右键行（数量 1）
            var targetCount = CountCheckedItems(grid);
            if (targetCount == 0)
                targetCount = rowData != null ? 1 : 0;

            // 没有任何操作目标（未勾选且右键空白处）→ 取消弹出
            if (targetCount == 0)
            {
                e.Handled = true;
                return;
            }

            // ContextMenuOpening 阶段 PlacementTarget 尚未赋值，Actions 绑定可能未解析到值，
            // 此时直接从 DataGrid 的 DataContext 读取 RowActions（约定属性）
            var actions = Actions ?? GetRowActionsFrom(grid.DataContext);
            BuildItems(actions, rowData, targetCount);

            // 权限/可见性过滤后无可显示项 → 取消弹出
            if (Items.Count == 0)
                e.Handled = true;
        }

        /// <summary>
        /// 从 ContextMenuOpening 事件的视觉源向上定位 DataGrid 行，返回其绑定的数据对象。
        /// </summary>
        private static object? GetRowData(ContextMenuEventArgs e)
        {
            DependencyObject? src = e.OriginalSource as DependencyObject;
            while (src != null && src is not DataGridRow)
                src = src is System.Windows.Media.Visual v
                    ? System.Windows.Media.VisualTreeHelper.GetParent(v)
                    : (src as System.Windows.FrameworkContentElement)?.Parent;
            return (src as DataGridRow)?.DataContext;
        }

        /// <summary>
        /// 从 DataGrid 的 DataContext 读取 RowActions 集合（约定属性名）。
        /// </summary>
        private static IEnumerable? GetRowActionsFrom(object? dataContext)
        {
            var prop = dataContext?.GetType().GetProperty("RowActions", BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(dataContext) as IEnumerable;
        }

        /// <summary>
        /// 按当前权限与可见性谓词重建菜单项（支持子菜单递归生成）。
        /// </summary>
        private void BuildItems(IEnumerable? actions, object? rowData, int targetCount)
        {
            Items.Clear();

            var session = AppHelper.GetService<IUserSessionService>();
            if (actions == null)
                return;

            foreach (var actionObj in actions)
            {
                if (actionObj is not RowAction action)
                    continue;

                if (!string.IsNullOrEmpty(action.PermCode) && session?.HasPerm(action.PermCode) != true)
                    continue;

                if (action.VisibleWhen != null && !action.VisibleWhen(targetCount))
                    continue;

                var menuItem = new MenuItem
                {
                    Header = action.Header,
                    Command = action.Command!,
                    // RowAction 未显式指定参数时，传递右键所在行的数据对象，
                    // 命令侧可将其作为"无勾选时的默认操作目标"
                    CommandParameter = action.CommandParameter ?? rowData,
                    Style = (Style?)Resources["RowMenuItemStyle"]
                };

                // 有子菜单：递归生成子项，父级不触发 Command（仅作分组入口）
                var children = action.ChildrenFactory != null
                    ? action.ChildrenFactory(rowData)
                    : action.Children;
                if (children != null && children.Count > 0)
                {
                    menuItem.Command = null;
                    menuItem.CommandParameter = null;
                    foreach (var child in BuildChildItems(children, rowData, targetCount))
                        menuItem.Items.Add(child);
                }

                Items.Add(menuItem);
            }
        }

        /// <summary>
        /// 递归生成子菜单项（子项同样受权限与可见性谓词约束）。
        /// </summary>
        private IEnumerable<MenuItem> BuildChildItems(IReadOnlyList<RowAction> actions, object? rowData, int targetCount)
        {
            var session = AppHelper.GetService<IUserSessionService>();
            foreach (var action in actions)
            {
                if (!string.IsNullOrEmpty(action.PermCode) && session?.HasPerm(action.PermCode) != true)
                    continue;

                if (action.VisibleWhen != null && !action.VisibleWhen(targetCount))
                    continue;

                var menuItem = new MenuItem
                {
                    Header = action.Header,
                    Command = action.Command!,
                    CommandParameter = action.CommandParameter ?? rowData,
                    Style = (Style?)Resources["RowMenuItemStyle"]
                };

                if (action.Children != null && action.Children.Count > 0)
                {
                    menuItem.Command = null;
                    menuItem.CommandParameter = null;
                    foreach (var child in BuildChildItems(action.Children, rowData, targetCount))
                        menuItem.Items.Add(child);
                }

                yield return menuItem;
            }
        }

        /// <summary>
        /// 统计 DataGrid 绑定数据中勾选行的数量（约定数据对象含 bool IsSelected 属性）。
        /// </summary>
        private static int CountCheckedItems(DataGrid grid)
        {
            if (grid.ItemsSource is not IEnumerable items)
                return 0;

            var count = 0;
            foreach (var item in items)
            {
                var prop = item?.GetType().GetProperty("IsSelected", BindingFlags.Public | BindingFlags.Instance);
                if (prop?.GetValue(item) is true)
                    count++;
            }

            return count;
        }
    }
}
