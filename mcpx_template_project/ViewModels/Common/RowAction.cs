using System.Windows.Input;

namespace mcpx_template_project.ViewModels.Common
{
    /// <summary>
    /// 右键菜单操作项：由页面的 ViewModel 提供操作集合，
    /// 供 <c>RowContextMenu</c> 控件生成菜单项。
    /// </summary>
    /// <author>Rudeus</author>
    public class RowAction
    {
        /// <summary>菜单显示文本</summary>
        public string Header { get; }

        /// <summary>点击菜单项执行的命令（带子菜单的父项可为 null）</summary>
        public ICommand? Command { get; }

        /// <summary>命令参数（批量操作通常由命令自身读取选中集合，可为 null）</summary>
        public object? CommandParameter { get; }

        /// <summary>按钮级权限标识（sys_menu.perms），为空表示不做权限过滤</summary>
        public string? PermCode { get; }

        /// <summary>
        /// 菜单项可见性谓词：参数为本次操作目标数量（勾选行数；无勾选且右键在行上时为 1），
        /// 返回 false 时该项不生成。为 null 表示始终可见。
        /// </summary>
        public Func<int, bool>? VisibleWhen { get; }

        /// <summary>
        /// 子菜单项集合：非空时该 RowAction 渲染为带子菜单的 MenuItem（父级不触发 Command）。
        /// </summary>
        public IReadOnlyList<RowAction>? Children { get; }

        /// <summary>
        /// 动态子菜单工厂：入参为右键所在行的数据对象（rowData），返回本次应展示的子菜单项集合。
        /// 用于子菜单内容依赖具体行数据（如"列印"按目录实际文件动态列项）的场景。
        /// 返回 null 或空集合表示无子菜单。优先级高于 <see cref="Children"/>。
        /// </summary>
        public Func<object?, IReadOnlyList<RowAction>?>? ChildrenFactory { get; }

        public RowAction(string header, ICommand? command, string? permCode = null,
            object? commandParameter = null, Func<int, bool>? visibleWhen = null,
            IReadOnlyList<RowAction>? children = null,
            Func<object?, IReadOnlyList<RowAction>?>? childrenFactory = null)
        {
            Header = header;
            Command = command;
            PermCode = permCode;
            CommandParameter = commandParameter;
            VisibleWhen = visibleWhen;
            Children = children;
            ChildrenFactory = childrenFactory;
        }
    }
}
