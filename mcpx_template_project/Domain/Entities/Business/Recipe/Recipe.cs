using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Enums;

namespace mcpx_template_project.Domain.Entities.Business.Recipe
{
    /// <summary>
    /// 配方实体
    /// recipe
    /// </summary>
    [Table("recipe")]
    public class Recipe : BaseEntity
    {
        /// <summary>主键</summary>
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>配方名称</summary>
        [Required]
        [MaxLength(100)]
        [Column("recipe_name")]
        public string RecipeName { get; set; } = string.Empty;

        /// <summary>一段上热压轮温度（15-130）</summary>
        [Column("upper_lami_tmp_stage1")]
        [PlcRead(PlcDevice.D, "2034", DisplayName = "一段上热压轮温度")]
        [PlcWrite(PlcDevice.D, "3700")]
        public int UpperLamiTmp_Stage1 { get; set; }

        /// <summary>一段下热压轮温度（15-130）</summary>
        [Column("lower_lami_tmp_stage1")]
        [PlcRead(PlcDevice.D, "2035", DisplayName = "一段下热压轮温度")]
        [PlcWrite(PlcDevice.D, "3701")]
        public int LowerLamiTmp_Stage1 { get; set; }

        /// <summary>二段上热压轮温度（15-130）</summary>
        [Column("upper_lami_tmp_stage2")]
        [PlcRead(PlcDevice.D, "2036", DisplayName = "二段上热压轮温度")]
        [PlcWrite(PlcDevice.D, "3713")]
        public int UpperLamiTmp_Stage2 { get; set; }

        /// <summary>二段下热压轮温度（115-130）</summary>
        [Column("lower_lami_tmp_stage2")]
        [PlcRead(PlcDevice.D, "2037", DisplayName = "二段下热压轮温度")]
        [PlcWrite(PlcDevice.D, "3714")]
        public int LowerLamiTmp_Stage2 { get; set; }

        /// <summary>膜宽（枚举：0=<0.6mm 1=<24mil 2=>=0.6mm 3=>=24mil）</summary>
        [Column("membrane_width")]
        [PlcRead(PlcDevice.D, "220", DisplayName = "膜宽")]
        [PlcWrite(PlcDevice.D, "3712")]
        public MembraneWidthRange MembraneWidth { get; set; }

        /// <summary>出板温度（15~100）</summary>
        [Column("panel_out_tmp")]
        [PlcRead(PlcDevice.D, "2032", DisplayName = "出板温度")]
        [PlcWrite(PlcDevice.D, "3721")]
        public int PanelOutTmp { get; set; }

        /// <summary>上贴膜温度（15~100）</summary>
        [Column("upper_tacking_tmp")]
        [PlcRead(PlcDevice.D, "2030", DisplayName = "上贴膜温度")]
        [PlcWrite(PlcDevice.D, "3707")]
        public int UpperTackingTmp { get; set; }

        /// <summary>下贴膜温度（15~100）</summary>
        [Column("lower_tacking_tmp")]
        [PlcRead(PlcDevice.D, "2031", DisplayName = "下贴膜温度")]
        [PlcWrite(PlcDevice.D, "3708")]
        public int LowerTackingTmp { get; set; }

        /// <summary>输送速度（1~5）</summary>
        [Column("drv_speed")]
        [PlcRead(PlcDevice.D, "600", DisplayName = "输送速度")]
        [PlcWrite(PlcDevice.D, "3702")]
        public double DrvSpeed { get; set; }

        /// <summary>上贴膜压力（0~5）</summary>
        [Column("upper_tacking_pressure")]
        [PlcRead(PlcDevice.D, "760", DisplayName = "上贴膜压力")]
        [PlcWrite(PlcDevice.D, "3720")]
        public double UpperTackingPressure { get; set; }

        /// <summary>前留铜（0~49.9）</summary>
        [Column("front_cu_pos")]
        [PlcRead(PlcDevice.D, "603", DisplayName = "前留铜")]
        [PlcWrite(PlcDevice.D, "3703")]
        public double FrontCuPos { get; set; }

        /// <summary>后留铜（0~12.9）</summary>
        [Column("rear_cu_pos")]
        [PlcRead(PlcDevice.D, "605", DisplayName = "后留铜")]
        [PlcWrite(PlcDevice.D, "3704")]
        public double RearCuPos { get; set; }

        /// <summary>一段压轮压力（0.3~5.0）</summary>
        [Column("roller_pressure_stage1")]
        [PlcRead(PlcDevice.D, "730", DisplayName = "一段压轮压力")]
        [PlcWrite(PlcDevice.D, "3709")]
        public double RollerPressure_Stage1 { get; set; }

        /// <summary>二段压轮压力（0.3~5.0）</summary>
        [Column("roller_pressure_stage2")]
        [PlcRead(PlcDevice.D, "770", DisplayName = "二段压轮压力")]
        [PlcWrite(PlcDevice.D, "3719")]
        public double RollerPressure_Stage2 { get; set; }

        /// <summary>贴膜时间（0.3~7）</summary>
        [Column("duration_film")]
        [PlcRead(PlcDevice.D, "221", DisplayName = "贴膜时间")]
        [PlcWrite(PlcDevice.D, "3710")]
        public double DurationFilm { get; set; }

        /// <summary>板厚（0.5~8）</summary>
        [Column("plate_thickness")]
        [PlcRead(PlcDevice.D, "219", DisplayName = "板厚")]
        [PlcWrite(PlcDevice.D, "3711")]
        public double PlateThickness { get; set; }

        // ============ PLC 握手位点 ============
        //「配方写入握手」的 PLC 位，不属于配方业务数据，
        // 用 [NotMapped] 排除出 EF 映射，但保留 [PlcWrite] 让点位扫描器识别。

        /// <summary>通知 PLC 写配方的触发位 M2804（PC 置 1 后 PLC 开始处理）</summary>
        [NotMapped]
        [PlcWrite(PlcDevice.M, "2804", DisplayName = "写入触发位", Role = PlcWriteRole.Trigger)]
        public bool WriteTrigger { get; set; }

        /// <summary>PLC 回复位 M2101（0=无回应/处理中，1=ACK，2=NAK）</summary>
        [NotMapped]
        [PlcWrite(PlcDevice.M, "2101", DisplayName = "回复位", Role = PlcWriteRole.Reply)]
        public int WriteReply { get; set; }

        /// <summary>
        /// 工艺程序命令码 CCODE（S7F23 下发 / S7F26 返回时使用）。
        /// 每个配方对应一个命令码，标识该配方在 SECS S7 工艺程序中的命令。
        /// </summary>
        [Column("ccode")]
        [MaxLength(64)]
        public string? Ccode { get; set; }
    }
}
