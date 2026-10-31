using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.Business.Secs
{
    /// <summary>
    /// SECS/GEM Report 定义（数据采集组）。
    /// 一个 Report 对应 S6F11（Event Report）里可上报的一组 VID 集合，
    /// 其成员通过子表 secs_report_vid 关联。
    /// </summary>
    [Table("secs_report")]
    public class SecsReport : BaseEntity
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

        /// <summary>Report 编号（SECS 消息中的 RPTID，1~65535）</summary>
        [Required]
        [Column("report_id")]
        public int ReportId { get; set; }

        /// <summary>Report 显示名</summary>
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>是否启用（0=启用，1=停用；停用的 Report 不参与上报）</summary>
        [Required]
        [MaxLength(1)]
        [Column("status")]
        public string Status { get; set; } = "0";

        /// <summary>Report 下的 VID 明细（导航）</summary>
        public List<SecsReportVid> Vids { get; set; } = new();
    }
}
