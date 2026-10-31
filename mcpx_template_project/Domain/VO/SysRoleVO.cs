using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.Domain.VO
{
    public partial class SysRoleVO : ObservableObject
    {
        public long RoleId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string RoleKey { get; set; } = string.Empty;

        public int RoleSort { get; set; }

        public string? DataScope { get; set; }

        public int? MenuCheckStrictly { get; set; }

        public int? DeptCheckStrictly { get; set; }

        public string? Status { get; set; }

        public string? DelFlag { get; set; }

        public string? CreateBy { get; set; }

        public DateTime? CreateTime { get; set; }

        public string? Remark { get; set; }

        [ObservableProperty]
        private bool _isSelected;

        public bool IsAdmin => RoleKey == "admin";
    }
}
