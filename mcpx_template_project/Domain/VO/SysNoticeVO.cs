using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.Domain.VO
{
    public partial class SysNoticeVO : ObservableObject
    {
        public long NoticeId { get; set; }

        public string NoticeTitle { get; set; } = string.Empty;

        /// <summary>通知类型（0 通知 1 公告）</summary>
        public string NoticeType { get; set; } = "0";

        /// <summary>类型显示名（通知 / 公告）</summary>
        public string NoticeTypeName => NoticeType == "1" ? "公告" : "通知";

        public string? NoticeContent { get; set; }

        /// <summary>内容来源（人工 / S10F3）</summary>
        public string? Source { get; set; }

        public string? Status { get; set; }

        public string? CreateBy { get; set; }

        public DateTime? CreateTime { get; set; }

        public string? UpdateBy { get; set; }

        public DateTime? UpdateTime { get; set; }

        public string? Remark { get; set; }

        /// <summary>当前用户是否已读（列表页由 Service 按用户填充）</summary>
        [ObservableProperty]
        private bool _isRead;

        [ObservableProperty]
        private bool _isSelected;
    }
}
