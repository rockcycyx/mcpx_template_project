using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.Business.CommSetting
{
    /// <summary>
    /// 通讯设定主表实体
    /// 对应数据库表：comm_setting
    /// </summary>
    [Table("comm_setting")]
    public class CommSetting : BaseEntity
    {
        /// <summary>主键（自增）</summary>
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>通讯设定名称</summary>
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>IP 地址</summary>
        [Required]
        [MaxLength(64)]
        [Column("ip")]
        public string Ip { get; set; } = string.Empty;

        /// <summary>端口</summary>
        [Required]
        [Column("port")]
        public int Port { get; set; }

        /// <summary>启动连线模式：manual 手动 / auto 自动</summary>
        [Required]
        [MaxLength(10)]
        [Column("connect_mode")]
        public string ConnectMode { get; set; } = "manual";

        /// <summary>通讯检查周期（ms）</summary>
        [Column("check_cycle_ms")]
        public int CheckCycleMs { get; set; } = 1000;

        /// <summary>TimeOut 超时时间（ms）</summary>
        [Column("timeout_ms")]
        public int TimeoutMs { get; set; } = 3000;

        /// <summary>断线重试周期（s）</summary>
        [Column("retry_cycle_s")]
        public int RetryCycleS { get; set; } = 5;

        /// <summary>
        /// 【已弃用】运行状态：0 已停止，1 已连接。
        /// 连线状态现为纯内存态，由通讯引擎 <c>IPlcCommunicationService.IsConnected()</c> 实时维护，
        /// 不再写入数据库。此列仅保留以兼容旧数据库表结构，业务代码不得再读写它。
        /// </summary>
        [Required]
        [MaxLength(1)]
        [Column("status")]
        public string Status { get; set; } = "0";

        /// <summary>单身明细（区域配置）</summary>
        public List<CommSettingArea> Areas { get; set; } = new();
    }
}
