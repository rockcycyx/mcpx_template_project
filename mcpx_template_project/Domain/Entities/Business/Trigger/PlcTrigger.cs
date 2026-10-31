using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.Business.Trigger
{
    /// <summary>
    /// PLC 触发器配置实体（统一警报与事件）。
    /// 对应数据库表：plc_trigger。
    /// 每个触发器对应 PLC 上的一个点位，当点位满足触发条件时执行对应动作：
    ///   trigger_type=alarm → 弹窗提示；
    ///   trigger_type=event → 反射调用 action_key 对应的事件处理器。
    /// </summary>
    [Table("plc_trigger")]
    public class PlcTrigger : BaseEntity
    {
        /// <summary>主键（自增）</summary>
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>所属通讯设定ID（哪个设备 comm_setting.id）</summary>
        [Required]
        [Column("comm_setting_id")]
        public long CommSettingId { get; set; }

        /// <summary>类型（设备代号，如 D/W/M/X/Y）</summary>
        [Required]
        [MaxLength(8)]
        [Column("device_code")]
        public string DeviceCode { get; set; } = "M";

        /// <summary>点位地址（如 100、2034）</summary>
        [Required]
        [MaxLength(32)]
        [Column("address")]
        public string Address { get; set; } = string.Empty;

        /// <summary>触发器类型：alarm / event</summary>
        [Required]
        [MaxLength(8)]
        [Column("trigger_type")]
        public string TriggerType { get; set; } = "alarm";

        /// <summary>触发方式：on（上升沿）/ change（值变化）</summary>
        [Required]
        [MaxLength(8)]
        [Column("trigger_mode")]
        public string TriggerMode { get; set; } = "on";

        /// <summary>事件处理器 Key（仅 trigger_type=event 时有效，对应 PlcEventHandlerAttribute.Key）</summary>
        [MaxLength(64)]
        [Column("action_key")]
        public string? ActionKey { get; set; }

        /// <summary>警报类别（仅 trigger_type=alarm 时有效，对应 ALCD 字节 bit7-1，取值 0~8，见 AlarmCategory 枚举）</summary>
        [Column("alarm_category")]
        public int AlarmCategory { get; set; }

        /// <summary>是否上报 S5F1 警报（仅 trigger_type=alarm 时有效；1=上报，0=不上报）</summary>
        [Required]
        [MaxLength(1)]
        [Column("report_alarm")]
        public string ReportAlarm { get; set; } = "0";

        /// <summary>触发名 / 警报名</summary>
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>状态：是否启用监控（0=停用，1=启用）</summary>
        [Required]
        [MaxLength(1)]
        [Column("status")]
        public string Status { get; set; } = "1";

        /// <summary>通讯设定名称（联表 comm_setting.name，由列表 ViewModel 回填，用于表格展示）</summary>
        [NotMapped]
        public string CommSettingName { get; set; } = string.Empty;

        /// <summary>完整点位（设备代号 + 地址），用于显示与读取</summary>
        [NotMapped]
        public string FullAddress => $"{DeviceCode}{Address}";

        /// <summary>是否为警报类型</summary>
        [NotMapped]
        public bool IsAlarm => string.Equals(TriggerType, "alarm", StringComparison.OrdinalIgnoreCase);

        /// <summary>是否为事件类型</summary>
        [NotMapped]
        public bool IsEvent => string.Equals(TriggerType, "event", StringComparison.OrdinalIgnoreCase);

        /// <summary>是否需要上报 S5F1（警报类型 + 开启上报）；ALID 直接使用触发器 Id</summary>
        [NotMapped]
        public bool ShouldReportAlarm =>
            IsAlarm && string.Equals(ReportAlarm, "1", StringComparison.OrdinalIgnoreCase);
    }
}
