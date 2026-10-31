using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.ViewModels.Admin.Notice
{
    /// <summary>通知详情查看弹窗 ViewModel（只读展示）。</summary>
    public partial class NoticeDetailViewModel : ObservableObject
    {
        public SysNoticeVO Notice { get; }

        public string NoticeTypeName => Notice?.NoticeTypeName ?? string.Empty;
        public string NoticeTitle => Notice?.NoticeTitle ?? string.Empty;
        public string NoticeContent => Notice?.NoticeContent ?? string.Empty;
        public string? CreateBy => Notice?.CreateBy;
        public DateTime? CreateTime => Notice?.CreateTime;
        public string? Source => Notice?.Source;

        public event Action<bool>? RequestClose;

        public NoticeDetailViewModel(SysNoticeVO notice)
        {
            Notice = notice;
        }

        [RelayCommand]
        private void Close()
        {
            RequestClose?.Invoke(false);
        }
    }
}
