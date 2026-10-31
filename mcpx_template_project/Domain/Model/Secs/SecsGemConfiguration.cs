using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.Domain.Model.Secs
{
    /// <summary>
    /// SECS/GEM 通讯配置（单例，生命周期独立于窗体，不随窗体关闭而销毁）。
    /// 本项目作为 Equipment 端（Passive，被动等待 Host 连接）。
    /// </summary>
    public partial class SecsGemConfiguration : ObservableObject
    {
        /// <summary>本机监听 IP 地址（Passive 端绑定地址）</summary>
        [ObservableProperty]
        private string _ipAddress = "0.0.0.0";

        /// <summary>本机监听端口</summary>
        [ObservableProperty]
        private int _port = 5000;

        /// <summary>设备 ID（DeviceId，Equipment 端通常为 0）</summary>
        [ObservableProperty]
        private ushort _deviceId = 0;

        /// <summary>启动连接模式：manual 手动 / auto 自动（登录后自动开放监听）</summary>
        [ObservableProperty]
        private string _connectMode = "manual";

        /// <summary>T3 回复超时（ms），默认 45000</summary>
        [ObservableProperty]
        private int _t3 = 45000;

        /// <summary>T5 连接建立超时（ms），默认 10000</summary>
        [ObservableProperty]
        private int _t5 = 10000;

        /// <summary>T6 控制消息超时（ms），默认 5000</summary>
        [ObservableProperty]
        private int _t6 = 5000;

        /// <summary>T7 选择状态超时（ms），默认 10000</summary>
        [ObservableProperty]
        private int _t7 = 10000;

        /// <summary>T8 分片接收超时（ms），默认 5000</summary>
        [ObservableProperty]
        private int _t8 = 5000;

        /// <summary>是否自动模式（登录后自动开放）</summary>
        public bool IsAuto => string.Equals(ConnectMode, "auto", StringComparison.OrdinalIgnoreCase);
    }
}
