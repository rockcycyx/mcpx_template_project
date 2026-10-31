using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.Domain.VO
{
    /// <summary>
    /// 通讯设定主表视图对象
    /// </summary>
    public partial class CommSettingVO : ObservableObject
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Ip { get; set; } = string.Empty;

        public int Port { get; set; }

        /// <summary>启动连线模式：manual 手动 / auto 自动</summary>
        public string ConnectMode { get; set; } = "manual";

        public int CheckCycleMs { get; set; }

        public int TimeoutMs { get; set; }

        public int RetryCycleS { get; set; }

        /// <summary>
        /// 运行状态（纯内存态）：0 已停止，1 已连接。
        /// 由列表 ViewModel 从通讯引擎 IsConnected() 实时回填，不落库。
        /// </summary>
        [ObservableProperty]
        private string _status = "0";

        public string? Remark { get; set; }

        public string? CreateBy { get; set; }

        public DateTime? CreateTime { get; set; }

        public string? UpdateBy { get; set; }

        public DateTime? UpdateTime { get; set; }

        public List<CommSettingAreaVO> Areas { get; set; } = new();

        /// <summary>是否运行中（已连接）</summary>
        public bool IsRunning => Status == "1";

        /// <summary>连线模式中文描述</summary>
        public string ConnectModeText => ConnectMode == "auto" ? "自动" : "手动";

        partial void OnStatusChanged(string value) => OnPropertyChanged(nameof(IsRunning));
    }
}
