using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcpx_template_project.Domain.Entities.System
{
    /// <summary>
    /// 通知公告已读记录实体类
    /// 对应数据库表：sys_notice_read
    /// 记录「某个用户已读某条通知」，用于侧边栏未读角标与列表已读状态。
    /// </summary>
    [Table("sys_notice_read")]
    public class SysNoticeRead
    {
        /// <summary>通知ID（联合主键）</summary>
        [Column("notice_id")]
        public long NoticeId { get; set; }

        /// <summary>用户ID（联合主键）</summary>
        [Column("user_id")]
        public long UserId { get; set; }

        /// <summary>阅读时间</summary>
        [Column("read_time")]
        public DateTime? ReadTime { get; set; }
    }
}
