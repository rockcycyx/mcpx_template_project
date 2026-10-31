using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.Domain.VO
{
    public partial class SysPostVO : ObservableObject
    {
        public long PostId { get; set; }

        public string PostCode { get; set; } = string.Empty;

        public string PostName { get; set; } = string.Empty;

        public int PostSort { get; set; }

        public string? Status { get; set; }

        public string? CreateBy { get; set; }

        public DateTime? CreateTime { get; set; }

        public string? UpdateBy { get; set; }

        public DateTime? UpdateTime { get; set; }

        public string? Remark { get; set; }

        [ObservableProperty]
        private bool _isSelected;
    }
}
