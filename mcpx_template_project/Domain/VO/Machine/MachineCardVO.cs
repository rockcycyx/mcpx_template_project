using CommunityToolkit.Mvvm.ComponentModel;
using mcpx_template_project.Domain.Enums;

namespace mcpx_template_project.Domain.VO.Machine
{
    /// <summary>
    /// 首页机台卡片视图对象：一台机台的所有展示字段。
    /// 值由首页 ViewModel 从通讯缓存读取（不落库），字段属性变更驱动界面实时刷新。
    /// </summary>
    public partial class MachineCardVO : ObservableObject
    {
        /// <summary>机台 ID</summary>
        public long Id { get; set; }

        /// <summary>机台名称（显示为「机台 #id」，或联表通讯设定名称）</summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>关联通讯设定名称（comm_setting.name）</summary>
        [ObservableProperty]
        private string _commSettingName = string.Empty;

        /// <summary>是否已连接</summary>
        [ObservableProperty]
        private bool _isConnected;

        // ============ SECS/GEM 连接状态 ============

        /// <summary>是否关联了 SECS/GEM 实例（comm_setting_id 在 secs_instance 中有对应实例）</summary>
        [ObservableProperty]
        private bool _hasSecsInstance;

        /// <summary>关联的 SECS/GEM 实例名称</summary>
        [ObservableProperty]
        private string _secsInstanceName = string.Empty;

        /// <summary>SECS/GEM 是否已通信（COMMUNICATING 状态）</summary>
        [ObservableProperty]
        private bool _secsCommunicating;

        /// <summary>SECS/GEM 连接状态文本（未关联/未启动/监听中/已通信/...）</summary>
        [ObservableProperty]
        private string _secsStateText = "无 SECS 实例";

        /// <summary>SECS/GEM 控制状态（GEM Control State：设备离线/在线本地/在线远程等）</summary>
        [ObservableProperty]
        private SecsControlState _secsControlState = SecsControlState.EquipmentOffline;

        /// <summary>控制状态显示文本</summary>
        public string SecsControlStateText => SecsControlState switch
        {
            SecsControlState.OnlineRemote => "OnLine Remote",
            SecsControlState.OnlineLocal => "OnLine Local",
            SecsControlState.HostOffline => "OffLine",
            SecsControlState.AttemptOnline => "AttemptOnline",
            _ => "设备离线"
        };

        // ============ 状态 ============

        /// <summary>机台状态（停機/待機/自動運轉）</summary>
        [ObservableProperty]
        private MachineState _machineState = MachineState.Stop;

        /// <summary>机台状态显示文本</summary>
        public string MachineStateText => MachineState switch
        {
            MachineState.Running => "自動運轉",
            MachineState.Idle => "待機",
            _ => "停機"
        };

        // ============ 料号 / 批号 ============

        [ObservableProperty] private string _objectId = string.Empty;
        [ObservableProperty] private string _lotId = string.Empty;

        // ============ 批量生产 ============

        [ObservableProperty] private int _currentBatchCount;
        [ObservableProperty] private int _targetBatchCount;

        /// <summary>批量进度文本（当前 / 设定）</summary>
        public string BatchProgressText => $"{CurrentBatchCount} / {TargetBatchCount}";

        // ============ 灯 / 蜂鸣器 ============

        [ObservableProperty] private BlueLampState _blueLamp = BlueLampState.Off;

        /// <summary>蓝灯显示文本</summary>
        public string BlueLampText => BlueLamp switch
        {
            BlueLampState.On => "常亮",
            BlueLampState.Blink => "闪烁",
            _ => "灯灭"
        };

        [ObservableProperty] private BuzzerState _buzzer = BuzzerState.Off;

        /// <summary>蜂鸣器显示文本</summary>
        public string BuzzerText => Buzzer == BuzzerState.On ? "蜂鸣" : "静音";

        // ============ 产量 / 功率 / 使用次数 ============

        [ObservableProperty] private float _effectivePower;

        [ObservableProperty] private float _totalProduction;

        [ObservableProperty] private float _dailyProduction;

        [ObservableProperty] private int _cutterUseCount;

        [ObservableProperty] private int _upperRollerTimeStage1;

        [ObservableProperty] private int _lowerRollerTimeStage1;

        [ObservableProperty] private int _upperRollerTimeStage2;

        [ObservableProperty] private int _lowerRollerTimeStage2;

        [ObservableProperty] private int _filmBarUseCount;

        /// <summary>最近刷新时间文本</summary>
        [ObservableProperty]
        private string _lastRefresh = string.Empty;

        // 状态变化时刷新派生文本
        partial void OnSecsControlStateChanged(SecsControlState value) => OnPropertyChanged(nameof(SecsControlStateText));
        partial void OnMachineStateChanged(MachineState value) => OnPropertyChanged(nameof(MachineStateText));
        partial void OnBlueLampChanged(BlueLampState value) => OnPropertyChanged(nameof(BlueLampText));
        partial void OnBuzzerChanged(BuzzerState value) => OnPropertyChanged(nameof(BuzzerText));
        partial void OnCurrentBatchCountChanged(int value) => OnPropertyChanged(nameof(BatchProgressText));
        partial void OnTargetBatchCountChanged(int value) => OnPropertyChanged(nameof(BatchProgressText));
    }
}
