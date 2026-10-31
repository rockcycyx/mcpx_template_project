using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.Business.Secs
{
    /// <summary>
    /// SECS/GEM 实例（Equipment/Passive 端通讯会话）。
    /// 对应数据库表：secs_instance。
    /// 一个实例代表一个独立的 SECS 会话（本机监听 IP/Port，等待 Host 连接），
    /// 通讯参数从 appsettings.json 的 "SecsGem" 单例迁入本表，实现多实例数据表驱动。
    /// 通过 comm_setting_id 关联 PLC 通讯设定（可空：source=db 的 VID 或纯上报实例不依赖 PLC）。
    /// </summary>
    [Table("secs_instance")]
    public class SecsInstance : BaseEntity
    {
        /// <summary>主键（自增，同时作为通讯层 deviceKey 的稳定标识）</summary>
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>实例名称</summary>
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>本机监听 IP 地址（Passive 端绑定地址）</summary>
        [Required]
        [MaxLength(64)]
        [Column("ip_address")]
        public string IpAddress { get; set; } = "0.0.0.0";

        /// <summary>本机监听端口</summary>
        [Required]
        [Column("port")]
        public int Port { get; set; } = 5000;

        /// <summary>设备 ID（DeviceId，Equipment 端通常为 0）</summary>
        [Column("device_id")]
        public ushort DeviceId { get; set; }

        /// <summary>启动连接模式：manual 手动 / auto 自动（登录后自动开放监听）</summary>
        [Required]
        [MaxLength(10)]
        [Column("connect_mode")]
        public string ConnectMode { get; set; } = "manual";

        /// <summary>T3 回复超时（ms），默认 45000</summary>
        [Column("t3")]
        public int T3 { get; set; } = 45000;

        /// <summary>T5 连接建立超时（ms），默认 10000</summary>
        [Column("t5")]
        public int T5 { get; set; } = 10000;

        /// <summary>T6 控制消息超时（ms），默认 5000</summary>
        [Column("t6")]
        public int T6 { get; set; } = 5000;

        /// <summary>T7 选择状态超时（ms），默认 10000</summary>
        [Column("t7")]
        public int T7 { get; set; } = 10000;

        /// <summary>T8 分片接收超时（ms），默认 5000</summary>
        [Column("t8")]
        public int T8 { get; set; } = 5000;

        /// <summary>关联的 PLC 通讯设定 ID（可空；指向 comm_setting.id）</summary>
        [Column("comm_setting_id")]
        public long? CommSettingId { get; set; }

        /// <summary>是否启用（0=启用，1=停用；停用的实例不参与自动启动）</summary>
        [Required]
        [MaxLength(1)]
        [Column("status")]
        public string Status { get; set; } = "0";

        /// <summary>是否自动模式（登录后自动开放）</summary>
        [NotMapped]
        public bool IsAuto => string.Equals(ConnectMode, "auto", StringComparison.OrdinalIgnoreCase);
    }
}
