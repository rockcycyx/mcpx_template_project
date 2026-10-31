using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcpx_template_project.Domain.Entities.Business.Secs
{
    /// <summary>
    /// CEID ↔ Report 关联子表。
    /// 对应数据库表：secs_ceid_report。
    /// 定义某个收集事件（CEID）发生时需要上报哪些 Report。
    /// </summary>
    [Table("secs_ceid_report")]
    public class SecsCeidReport
    {
        /// <summary>主键（自增）</summary>
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>所属 CEID（关联 secs_ceid.id）</summary>
        [Required]
        [Column("ceid_id")]
        public long CeidId { get; set; }

        /// <summary>关联 Report（关联 secs_report.id）</summary>
        [Required]
        [Column("report_id")]
        public long ReportId { get; set; }

        /// <summary>排序号</summary>
        [Column("sort")]
        public int Sort { get; set; }

        /// <summary>所属 CEID（导航）</summary>
        [ForeignKey("CeidId")]
        public SecsCeid? Ceid { get; set; }

        /// <summary>关联 Report（导航）</summary>
        [ForeignKey("ReportId")]
        public SecsReport? Report { get; set; }
    }
}
