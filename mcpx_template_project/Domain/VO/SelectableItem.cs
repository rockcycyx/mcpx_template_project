using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.Domain.VO
{
    /// <summary>
    /// 双列表选择器中的通用数据项
    /// </summary>
    public partial class SelectableItem : ObservableObject
    {
        public long Id { get; set; }

        public string DisplayText { get; set; } = string.Empty;

        [ObservableProperty]
        private bool _isSelected;
    }
}