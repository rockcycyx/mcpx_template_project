using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.System
{
    /// <summary>
    /// 通知公告实体类
    /// 对应数据库表：sys_notice
    /// </summary>
    [Table("sys_notice")]
    public class SysNotice : BaseEntity
    {
        /// <summary>
        /// 通知ID（主键，自增）
        /// </summary>
        [Key]
        [Column("notice_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long NoticeId { get; set; }

        /// <summary>
        /// 通知标题
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Column("notice_title")]
        public string NoticeTitle { get; set; } = string.Empty;

        /// <summary>
        /// 通知类型（0 通知 1 公告）
        /// </summary>
        [Required]
        [MaxLength(1)]
        [Column("notice_type")]
        public string NoticeType { get; set; } = "0";

        /// <summary>
        /// 通知内容
        /// </summary>
        [Column("notice_content")]
        public string? NoticeContent { get; set; }

        /// <summary>
        /// 内容来源（人工 / S10F3 终端消息），用于区分通知公告是人工创建还是 Host 下发
        /// </summary>
        [MaxLength(20)]
        [Column("source")]
        public string? Source { get; set; }

        /// <summary>
        /// 状态（0 正常 1 停用）
        /// </summary>
        [Required]
        [MaxLength(1)]
        [Column("status")]
        public string Status { get; set; } = "0";
    }
}
