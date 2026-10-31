using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.System
{
    /// <summary>
    /// 部门表实体类
    /// </summary>
    [Table("sys_dept")]
    public class SysDept : BaseEntity
    {
        /// <summary>
        /// 部门ID（主键，自增）
        /// </summary>
        [Key]
        [Column("dept_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DeptId { get; set; }

        /// <summary>
        /// 父部门ID
        /// </summary>
        [Column("parent_id")]
        public long? ParentId { get; set; }

        /// <summary>
        /// 祖级列表
        /// </summary>
        [MaxLength(50)]
        [Column("ancestors")]
        public string? Ancestors { get; set; }

        /// <summary>
        /// 部门名称
        /// </summary>
        [Required]
        [MaxLength(30)]
        [Column("dept_name")]
        public string DeptName { get; set; } = string.Empty;

        /// <summary>
        /// 显示顺序
        /// </summary>
        [Column("order_num")]
        public int? OrderNum { get; set; }

        /// <summary>
        /// 负责人
        /// </summary>
        [MaxLength(20)]
        [Column("leader")]
        public string? Leader { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [MaxLength(11)]
        [Column("phone")]
        public string? Phone { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        [MaxLength(50)]
        [Column("email")]
        public string? Email { get; set; }

        /// <summary>
        /// 部门状态（0正常 1停用）
        /// </summary>
        [MaxLength(1)]
        [Column("status")]
        public string? Status { get; set; }

        /// <summary>
        /// 删除标志（0代表存在 2代表删除）
        /// </summary>
        [MaxLength(1)]
        [Column("del_flag")]
        public string? DelFlag { get; set; }
    }
}