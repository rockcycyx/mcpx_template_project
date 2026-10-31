using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.Domain.VO
{
    public partial class SysOperLogVO : ObservableObject
    {
        public long OperId { get; set; }

        public string Title { get; set; } = string.Empty;

        public int BusinessType { get; set; }

        public string Method { get; set; } = string.Empty;

        public int OperatorType { get; set; }

        public string OperName { get; set; } = string.Empty;

        public string DeptName { get; set; } = string.Empty;

        public string OperIp { get; set; } = string.Empty;

        public string OperLocation { get; set; } = string.Empty;

        public string OperParam { get; set; } = string.Empty;

        public string JsonResult { get; set; } = string.Empty;

        public int Status { get; set; }

        public string ErrorMsg { get; set; } = string.Empty;

        public DateTime? OperTime { get; set; }

        public long CostTime { get; set; }

        [ObservableProperty]
        private bool _isSelected;

        public string BusinessTypeText => BusinessType switch
        {
            0 => "其它",
            1 => "新增",
            2 => "修改",
            3 => "删除",
            _ => "未知"
        };

        public string StatusText => Status == 0 ? "正常" : "异常";
    }
}
