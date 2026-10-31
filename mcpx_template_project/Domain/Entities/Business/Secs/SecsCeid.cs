using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.Business.Secs
{
    /// <summary>
    /// SECS/GEM 收集事件定义（Collection Event）。
    /// 对应数据库表：secs_ceid。
    /// CEID 对应 S6F11（Event Report）中的事件标识，事件发生时按关联的 Report 集合上报。
    /// 一个 CEID 可关联多个 Report，通过子表 secs_ceid_report 维护。
    /// </summary>
    [Table("secs_ceid")]
    public class SecsCeid : BaseEntity
    {
        /// <summary>主键（自增）</summary>
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>所属 SECS 实例 ID（关联 secs_instance.id）</summary>
        [Required]
        [Column("instance_id")]
        public long InstanceId { get; set; }

        /// <summary>CEID 编号（SECS 消息中的 CEID，1~65535）</summary>
        [Required]
        [Column("ceid")]
        public int Ceid { get; set; }

        /// <summary>事件显示名</summary>
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>是否启用（0=启用，1=停用；停用的事件不参与上报）</summary>
        [Required]
        [MaxLength(1)]
        [Column("status")]
        public string Status { get; set; } = "0";

        /// <summary>该 CEID 需要上报的 Report 集合（导航）</summary>
        public List<SecsCeidReport> Reports { get; set; } = new();
    }
}
