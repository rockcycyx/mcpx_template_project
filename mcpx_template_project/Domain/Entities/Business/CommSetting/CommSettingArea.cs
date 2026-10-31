using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mcpx_template_project.Domain.Entities.Business.CommSetting
{
    /// <summary>
    /// 通讯设定单身明细实体
    /// 对应数据库表：comm_setting_area
    /// </summary>
    [Table("comm_setting_area")]
    public class CommSettingArea
    {
        /// <summary>主键（自增）</summary>
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>所属通讯设定ID</summary>
        [Column("setting_id")]
        public long SettingId { get; set; }

        /// <summary>区域：AI / AO / DI / DO</summary>
        [Required]
        [MaxLength(10)]
        [Column("area_type")]
        public string AreaType { get; set; } = "AI";

        /// <summary>对应名称</summary>
        [MaxLength(100)]
        [Column("name")]
        public string? Name { get; set; }

        /// <summary>对应位置</summary>
        [MaxLength(100)]
        [Column("position")]
        public string? Position { get; set; }

        /// <summary>对应数量</summary>
        [Column("quantity")]
        public int Quantity { get; set; }

        /// <summary>排序号</summary>
        [Column("sort")]
        public int Sort { get; set; }

        /// <summary>所属通讯设定（导航）</summary>
        [ForeignKey("SettingId")]
        public CommSetting? Setting { get; set; }
    }
}
