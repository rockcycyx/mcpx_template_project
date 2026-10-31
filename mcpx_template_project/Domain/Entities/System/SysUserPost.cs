using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcpx_template_project.Domain.Entities.System
{
    /// <summary>
    /// 用户和岗位关联表实体类
    /// 对应数据库表：sys_user_post（联合主键）
    /// </summary>
    [Table("sys_user_post")]
    public class SysUserPost
    {
        /// <summary>
        /// 用户ID（联合主键）
        /// </summary>
        [Key]
        [Column("user_id", Order = 0)]
        public long UserId { get; set; }

        /// <summary>
        /// 岗位ID（联合主键）
        /// </summary>
        [Key]
        [Column("post_id", Order = 1)]
        public long PostId { get; set; }
    }
}
