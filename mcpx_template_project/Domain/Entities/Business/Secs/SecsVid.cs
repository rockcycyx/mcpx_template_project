using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.Business.Secs
{
    /// <summary>
    /// SECS/GEM VID 统一主表（数据表驱动，替代注解）。
    /// 对应数据库表：secs_vid。
    /// 通过 vid_type 区分三类变量：
    ///   SVID = 状态变量（Status Variable，Host 只读，S1F3/S1F11）；
    ///   DVID = 数据变量（Data Variable，Host 可读写，S2F13/S2F15/S2F29）；
    ///   ECID = 设备常量（Equipment Constant，Host 只读，S2F13/S2F15/S2F29，与 DVID 共用编号语义）。
    /// 值来源 source 区分：
    ///   db  = 值存数据库（本表或外部缓存），由业务代码维护；
    ///   plc = 值取自 PLC 点位（plc_address 指向地址，运行时以缓存为主）。
    /// </summary>
    [Table("secs_vid")]
    public class SecsVid : BaseEntity
    {
        /// <summary>主键（自增，内部行号，非 VID 编号）</summary>
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>所属 SECS 实例 ID（关联 secs_instance.id）</summary>
        [Required]
        [Column("instance_id")]
        public long InstanceId { get; set; }

        /// <summary>变量类别：SVID / DVID / ECID</summary>
        [Required]
        [Column("vid_type")]
        public SecsVidType VidType { get; set; } = SecsVidType.SVID;

        /// <summary>VID 编号（SECS 消息中的编号，1~65535；同 vid_type 内唯一）</summary>
        [Required]
        [Column("vid_id")]
        public int VidId { get; set; }

        /// <summary>变量显示名</summary>
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>数据类型（SECS 侧格式码，如 U1/U2/U4/I2/I4/F4/F8/B/A 等）</summary>
        [Required]
        [Column("data_type")]
        public SecsDataType DataType { get; set; } = SecsDataType.A;

        /// <summary>值来源：db / plc</summary>
        [Required]
        [MaxLength(8)]
        [Column("source")]
        public string Source { get; set; } = "db";

        /// <summary>PLC 点位地址（仅 source=plc 时有效，如 100、2034；source=db 时为 null）</summary>
        [MaxLength(32)]
        [Column("plc_address")]
        public string? PlcAddress { get; set; }

        /// <summary>PLC 设备代号（仅 source=plc 时有效，如 M/X/Y）</summary>
        [MaxLength(8)]
        [Column("device_code")]
        public string? DeviceCode { get; set; }

        /// <summary>固定值（仅 source=db 时有效，即值直接取该字段，无需 PLC 或外部维护）</summary>
        [Column("default_value")]
        public string? DefaultValue { get; set; }

        /// <summary>是否启用（0=启用，1=停用；停用的 VID 不参与上报/查询）</summary>
        [Required]
        [MaxLength(1)]
        [Column("status")]
        public string Status { get; set; } = "0";

        /// <summary>是否为 SVID</summary>
        [NotMapped]
        public bool IsSvid => VidType == SecsVidType.SVID;

        /// <summary>是否为 DVID</summary>
        [NotMapped]
        public bool IsDvid => VidType == SecsVidType.DVID;

        /// <summary>是否为 ECID（设备常量）</summary>
        [NotMapped]
        public bool IsEcid => VidType == SecsVidType.ECID;

        /// <summary>值是否来自 PLC 点位</summary>
        [NotMapped]
        public bool IsPlcSource => string.Equals(Source, "plc", StringComparison.OrdinalIgnoreCase);
    }
}
