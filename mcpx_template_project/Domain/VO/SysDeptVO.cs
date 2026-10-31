using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.Domain.VO
{
    public partial class SysDeptVO : ObservableObject
    {
        public long DeptId { get; set; }

        public long? ParentId { get; set; }

        public string? Ancestors { get; set; }

        public string DeptName { get; set; } = string.Empty;

        public int? OrderNum { get; set; }

        public string? Leader { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Status { get; set; }

        public string? DelFlag { get; set; }

        public string? CreateBy { get; set; }

        public DateTime? CreateTime { get; set; }

        public string? UpdateBy { get; set; }

        public DateTime? UpdateTime { get; set; }

        public ObservableCollection<SysDeptVO> Children { get; } = new();

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isExpanded = true;
    }
}
