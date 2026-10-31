using System;

namespace mcpx_template_project.Domain.Model
{
    public class SysRoleQuery
    {
        public string? RoleName { get; set; }

        public string? RoleKey { get; set; }

        public string? Status { get; set; }

        public DateTime? BeginTime { get; set; }

        public DateTime? EndTime { get; set; }
    }
}
