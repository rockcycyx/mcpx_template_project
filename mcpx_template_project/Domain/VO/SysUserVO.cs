using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace mcpx_template_project.Domain.VO
{
    public partial class SysUserVO : ObservableObject
    {
        public long UserId { get; set; }

        public long? DeptId { get; set; }

        public string? DeptName { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string NickName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Avatar { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Sex { get; set; }

        public string? Status { get; set; }

        public string? LoginIp { get; set; }

        public DateTime? LoginDate { get; set; }

        public string? CreateBy { get; set; }

        public DateTime? CreateTime { get; set; }

        public string? Remark { get; set; }

        [ObservableProperty]
        private bool _isSelected;

        public bool IsAdmin => string.Equals(UserName, "admin", StringComparison.OrdinalIgnoreCase);
    }
}
