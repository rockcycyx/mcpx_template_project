using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.System
{
    /// <summary>
    /// 角色信息表实体类
    /// 对应数据库表：sys_role
    /// </summary>
    [Table("sys_role")]
    public class SysRole : BaseEntity
    {
        /// <summary>
        /// 角色ID（主键，自增）
        /// </summary>
        [Key]
        [Column("role_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long RoleId { get; set; }

        /// <summary>
        /// 角色名称
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("role_name")]
        public string RoleName { get; set; } = string.Empty;

        /// <summary>
        /// 角色权限字符串
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Column("role_key")]
        public string RoleKey { get; set; } = string.Empty;

        /// <summary>
        /// 显示顺序
        /// </summary>
        [Required]
        [Column("role_sort")]
        public int RoleSort { get; set; }

        /// <summary>
        /// 数据范围（1：全部数据权限 2：自定数据权限 3：本部门数据权限 4：本部门及以下数据权限）
        /// </summary>
        [MaxLength(1)]
        [Column("data_scope")]
        public string? DataScope { get; set; }

        /// <summary>
        /// 菜单树选择项是否关联显示
        /// </summary>
        [Column("menu_check_strictly")]
        public int? MenuCheckStrictly { get; set; }

        /// <summary>
        /// 部门树选择项是否关联显示
        /// </summary>
        [Column("dept_check_strictly")]
        public int? DeptCheckStrictly { get; set; }

        /// <summary>
        /// 角色状态（0正常 1停用）
        /// </summary>
        [Required]
        [MaxLength(1)]
        [Column("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 删除标志（0代表存在 2代表删除）
        /// </summary>
        [MaxLength(1)]
        [Column("del_flag")]
        public string? DelFlag { get; set; }
    }
}