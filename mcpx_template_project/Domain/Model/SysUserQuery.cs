using System;

namespace mcpx_template_project.Domain.Model
{
    /// <summary>
    /// 用户查询参数模型，支持条件查询的复用
    /// </summary>
    public class SysUserQuery
    {
        public long? UserId { get; set; }

        public string? UserName { get; set; }

        public string? Status { get; set; }

        public string? PhoneNumber { get; set; }

        public DateTime? BeginTime { get; set; }

        public DateTime? EndTime { get; set; }

        public long? DeptId { get; set; }
    }
}
