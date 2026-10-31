using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Domain.Common;

namespace mcpx_template_project.Domain.Entities.Business.Machine
{
    /// <summary>
    /// 机台表实体。
    /// 对应数据库表：machine。
    /// 一台机台通过 comm_setting_id 关联一条 PLC 通讯设定，
    /// 机台的实时状态点位（机台状态/料号/批号/产量等）不落库，
    /// 由 <see cref="MachinePlc"/>（纯内存 PLC 读写模型）通过注解声明并实时从缓存读取。
    /// </summary>
    [Table("machine")]
    public class Machine : BaseEntity
    {
        /// <summary>主键（自增）</summary>
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>关联的 PLC 通讯设定 ID（comm_setting.id）</summary>
        [Required]
        [Column("comm_setting_id")]
        public long CommSettingId { get; set; }

        /// <summary>机台名称</summary>
        [Required]
        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
