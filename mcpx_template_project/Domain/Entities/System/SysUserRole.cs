using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcpx_template_project.Domain.Entities.System
{
    /// <summary>
    /// 用户和角色关联表实体类
    /// 对应数据库表：sys_user_role（联合主键）
    /// </summary>
    [Table("sys_user_role")]
    public class SysUserRole
    {
        /// <summary>
        /// 用户ID（联合主键）
        /// </summary>
        [Key]
        [Column("user_id", Order = 0)]
        public long UserId { get; set; }

        /// <summary>
        /// 角色ID（联合主键）
        /// </summary>
        [Key]
        [Column("role_id", Order = 1)]
        public long RoleId { get; set; }
    }
}
