using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.System
{
    /// <summary>
    /// 菜单权限表实体类
    /// 对应数据库表：sys_menu
    /// </summary>
    [Table("sys_menu")]
    public class SysMenu : BaseEntity
    {
        /// <summary>
        /// 菜单ID（主键，自增）
        /// </summary>
        [Key]
        [Column("menu_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long MenuId { get; set; }

        /// <summary>
        /// 菜单名称
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("menu_name")]
        public string MenuName { get; set; } = string.Empty;

        /// <summary>
        /// 父菜单ID
        /// </summary>
        [Column("parent_id")]
        public long? ParentId { get; set; }

        /// <summary>
        /// 显示顺序
        /// </summary>
        [Column("order_num")]
        public int? OrderNum { get; set; }

        /// <summary>
        /// 组件路径：对应 Page 类的全限定类型名，用于反射创建实例
        /// </summary>
        [MaxLength(255)]
        [Column("component")]
        public string? Component { get; set; }

        /// <summary>
        /// 菜单类型（M目录 C菜单 F按钮）
        /// </summary>
        [MaxLength(1)]
        [Column("menu_type")]
        public string? MenuType { get; set; }

        /// <summary>
        /// 菜单状态（0显示 1隐藏）
        /// </summary>
        [MaxLength(1)]
        [Column("visible")]
        public string? Visible { get; set; }

        /// <summary>
        /// 菜单状态（0正常 1停用）
        /// </summary>
        [MaxLength(1)]
        [Column("status")]
        public string? Status { get; set; }

        /// <summary>
        /// 权限标识
        /// </summary>
        [MaxLength(100)]
        [Column("perms")]
        public string? Perms { get; set; }

        /// <summary>
        /// 菜单图标
        /// </summary>
        [MaxLength(100)]
        [Column("icon")]
        public string? Icon { get; set; }
    }
}