using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcpx_template_project.Domain.Entities.Business.Secs
{
    /// <summary>
    /// Report ↔ VID 多对多子表。
    /// 对应数据库表：secs_report_vid。
    /// 定义某个 Report 包含哪些 VID（SVID/DVID），sort 控制上报顺序。
    /// </summary>
    [Table("secs_report_vid")]
    public class SecsReportVid
    {
        /// <summary>主键（自增）</summary>
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>所属 Report（关联 secs_report.id）</summary>
        [Required]
        [Column("report_id")]
        public long ReportId { get; set; }

        /// <summary>成员 VID（关联 secs_vid.id）</summary>
        [Required]
        [Column("vid_id")]
        public long VidId { get; set; }

        /// <summary>上报顺序（升序）</summary>
        [Column("sort")]
        public int Sort { get; set; }

        /// <summary>所属 Report（导航）</summary>
        [ForeignKey("ReportId")]
        public SecsReport? Report { get; set; }

        /// <summary>成员 VID（导航）</summary>
        [ForeignKey("VidId")]
        public SecsVid? Vid { get; set; }
    }
}
