using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace mcpx_template_project.Domain.VO
{
    public partial class SysMenuVO : ObservableObject
    {
        public long MenuId { get; set; }

        public string MenuName { get; set; } = string.Empty;

        public string? ParentName { get; set; }

        public long? ParentId { get; set; }

        public int? OrderNum { get; set; }

        /// <summary>
        /// 组件路径：对应 Page 类的全限定类型名，用于反射创建实例
        /// </summary>
        public string? Component { get; set; }

        /// <summary>
        /// 菜单类型（M目录 C菜单 F按钮）
        /// </summary>
        public string? MenuType { get; set; }

        /// <summary>
        /// 显示状态（0显示 1隐藏）
        /// </summary>
        public string? Visible { get; set; }

        /// <summary>
        /// 菜单状态（0正常 1停用）
        /// </summary>
        public string? Status { get; set; }

        public string? Perms { get; set; }

        public string? Icon { get; set; }

        public string? CreateBy { get; set; }

        public DateTime? CreateTime { get; set; }

        public string? UpdateBy { get; set; }

        public DateTime? UpdateTime { get; set; }

        public string? Remark { get; set; }

        [ObservableProperty]
        private ObservableCollection<SysMenuVO> _children = new();

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isExpanded = true;
    }
}
