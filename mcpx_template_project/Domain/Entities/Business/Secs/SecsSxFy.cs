using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.Business.Secs
{
    /// <summary>
    /// SECS/GEM SxF 消息权限表（数据表驱动）。
    /// 对应数据库表：secs_sxfy。
    /// 定义「作为 Passive（Equipment）端，某条 SxF 指令在哪个控制状态下允许被 Host 调用执行」。
    /// 四个状态列对应 <see cref="Domain.Enums.SecsControlState"/>：
    ///   offline      → EquipmentOffline（设备离线）
    ///   online       → OnlineLocal / OnlineRemote（在线，含本地/远程两类，通常为「在线」笼统概念）
    ///   online_local → OnlineLocal（在线-本地）
    ///   online_remote→ OnlineRemote（在线-远程）
    /// 说明：OffLine 对应 EquipmentOffline；OnLine 因 E30 在线细分为本地/远程，
    /// 故此处以 offline / online_local / online_remote 三列精确表达，
    /// 另设 online 列作为「在线」的聚合允许位（在线本地或在线远程均视为在线），
    /// 便于配置「只要在线就允许」的常见场景。
    /// </summary>
    [Table("secs_sxfy")]
    public class SecsSxFy : BaseEntity
    {
        /// <summary>主键（自增）</summary>
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>所属 SECS 实例 ID（关联 secs_instance.id）</summary>
        [Required]
        [Column("instance_id")]
        public long InstanceId { get; set; }

        /// <summary>消息 Stream（1~127）</summary>
        [Required]
        [Column("stream")]
        public byte Stream { get; set; }

        /// <summary>消息 Function（0~255）</summary>
        [Required]
        [Column("function")]
        public byte Function { get; set; }

        /// <summary>设备离线（EquipmentOffline）状态是否允许执行（0=禁止，1=允许）</summary>
        [Required]
        [Column("allow_offline")]
        public bool AllowOffline { get; set; }

        /// <summary>在线-本地（OnlineLocal）状态是否允许执行（0=禁止，1=允许）</summary>
        [Required]
        [Column("allow_online_local")]
        public bool AllowOnlineLocal { get; set; }

        /// <summary>在线-远程（OnlineRemote）状态是否允许执行（0=禁止，1=允许）</summary>
        [Required]
        [Column("allow_online_remote")]
        public bool AllowOnlineRemote { get; set; }

        /// <summary>是否启用（0=启用，1=停用；停用的权限条目不参与校验）</summary>
        [Required]
        [MaxLength(1)]
        [Column("status")]
        public string Status { get; set; } = "0";

        /// <summary>显示名（可选，用于界面展示 SxF 含义，如 "S1F3 状态查询"）</summary>
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>路由显示文本</summary>
        [NotMapped]
        public string SxF => $"S{Stream}F{Function}";
    }
}
