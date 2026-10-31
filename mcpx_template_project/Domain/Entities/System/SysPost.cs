using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.System
{
    /// <summary>
    /// 岗位信息表实体类
    /// 对应数据库表：sys_post
    /// </summary>
    [Table("sys_post")]
    public class SysPost : BaseEntity
    {
        /// <summary>
        /// 岗位ID（主键，自增）
        /// </summary>
        [Key]
        [Column("post_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PostId { get; set; }

        /// <summary>
        /// 岗位编码
        /// </summary>
        [Required]
        [MaxLength(64)]
        [Column("post_code")]
        public string PostCode { get; set; } = string.Empty;

        /// <summary>
        /// 岗位名称
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("post_name")]
        public string PostName { get; set; } = string.Empty;

        /// <summary>
        /// 显示顺序
        /// </summary>
        [Required]
        [Column("post_sort")]
        public int PostSort { get; set; }

        /// <summary>
        /// 状态（0正常 1停用）
        /// </summary>
        [Required]
        [MaxLength(1)]
        [Column("status")]
        public string Status { get; set; } = string.Empty;
    }
}