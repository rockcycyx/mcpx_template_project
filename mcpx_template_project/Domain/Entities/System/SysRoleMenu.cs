using System.ComponentModel.DataAnnotations.Schema;

namespace mcpx_template_project.Domain.Entities.System
{
    [Table("sys_role_menu")]
    public class SysRoleMenu
    {
        [Column("role_id")]
        public long RoleId { get; set; }

        [Column("menu_id")]
        public long MenuId { get; set; }
    }
}
