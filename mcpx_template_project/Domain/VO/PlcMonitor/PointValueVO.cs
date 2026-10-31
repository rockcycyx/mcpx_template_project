using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.Domain.VO
{
    /// <summary>
    /// PLC 点位实时值视图对象
    /// 一个 PointValueVO 对应一条区域配置（CommSettingArea），
    /// 其读取的多个值展开为 <see cref="Values"/> 子值列表，供界面以表格形式展开展示。
    /// </summary>
    public partial class PointValueVO : ObservableObject
    {
        /// <summary>所属通讯设定ID</summary>
        public long SettingId { get; set; }

        /// <summary>所属通讯设定名称</summary>
        public string SettingName { get; set; } = string.Empty;

        /// <summary>区域类型：AI / AO / DI / DO</summary>
        [ObservableProperty]
        private string _areaType = "AI";

        /// <summary>设备代号（D / W / M / X / Y ...）</summary>
        [ObservableProperty]
        private string? _deviceCode;

        /// <summary>PLC 起始地址</summary>
        [ObservableProperty]
        private string? _plcAddress;

        /// <summary>读取点数</summary>
        [ObservableProperty]
        private int _quantity;

        /// <summary>展开后的子值列表（每个元素对应一个PLC地址的当前值）</summary>
        public ObservableCollection<PointSubValueVO> Values { get; } = new();

        /// <summary>汇总显示文本（子值拼接，用于列表快速预览）</summary>
        [ObservableProperty]
        private string _value = string.Empty;

        /// <summary>最近一次读取时间</summary>
        [ObservableProperty]
        private DateTime _updateTime = DateTime.Now;

        /// <summary>是否正常读取（在线）</summary>
        [ObservableProperty]
        private bool _isOnline;

        /// <summary>错误信息（读取失败时填充）</summary>
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        /// <summary>值类型描述（位/字）</summary>
        [ObservableProperty]
        private string _dataType = string.Empty;

        /// <summary>计算属性：点位全称（设备代号+地址）</summary>
        public string FullAddress => $"{DeviceCode}{PlcAddress}";

        /// <summary>计算属性：状态文本</summary>
        public string StatusText => IsOnline ? "正常" : (!string.IsNullOrEmpty(ErrorMessage) ? "异常" : "离线");
    }

    /// <summary>
    /// PLC 点位子值：展开后单个地址的当前值。
    /// </summary>
    public partial class PointSubValueVO : ObservableObject
    {
        /// <summary>设备代号（D / W / M / X / Y ...）</summary>
        public string? DeviceCode { get; set; }

        /// <summary>该子值对应的PLC地址</summary>
        public string? PlcAddress { get; set; }

        /// <summary>当前值（位设备为 ON/OFF，字设备为数值）</summary>
        [ObservableProperty]
        private string _value = string.Empty;
    }
}
