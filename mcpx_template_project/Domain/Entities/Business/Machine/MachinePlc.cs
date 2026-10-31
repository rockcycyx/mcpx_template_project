using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Domain.Enums;

namespace mcpx_template_project.Domain.Entities.Business.Machine
{
    /// <summary>
    /// 机台类（纯内存 PLC 读写模型，不持久化到数据库）。
    /// 承载机台各状态点位的 PLC 地址声明（读点 [PlcRead] / 写点 [PlcWrite]），
    /// 由通用 <see cref="Service.Business.Plc.IPlcObjectService"/> 门面扫描注解，
    /// 从通讯缓存读取当前值、下发写值，业务/界面无需关心底层地址。
    ///
    /// 注意：本类属性均不落库（无 [Table]/[Column]），仅作为点位声明的载体。
    /// </summary>
    public class MachinePlc
    {
        // ============ 只读状态点 ============

        /// <summary>机台状态（D3114）：停機=0 / 待機=1 / 自動運轉=2</summary>
        [PlcRead(PlcDevice.D, "3114", DisplayName = "机台状态")]
        public MachineState MachineState { get; set; }

        /// <summary>当前批量生产数量（D3109，只读）</summary>
        [PlcRead(PlcDevice.D, "3109", DisplayName = "当前批量生产数量")]
        public int CurrentBatchCount { get; set; }

        // ============ 读写状态点 ============

        /// <summary>ObjectID（料号），读写点 3500</summary>
        [PlcRead(PlcDevice.D, "3500", DisplayName = "ObjectID（料号）")]
        [PlcWrite(PlcDevice.D, "3500", DisplayName = "ObjectID（料号）")]
        public string ObjectId { get; set; } = string.Empty;

        /// <summary>LotID（批号），读写点 3510</summary>
        [PlcRead(PlcDevice.D, "3510", DisplayName = "LotID（批号）")]
        [PlcWrite(PlcDevice.D, "3510", DisplayName = "LotID（批号）")]
        public string LotId { get; set; } = string.Empty;

        /// <summary>设定批量生产数量（D3009，读写）</summary>
        [PlcRead(PlcDevice.D, "3009", DisplayName = "设定批量生产数量")]
        [PlcWrite(PlcDevice.D, "3009", DisplayName = "设定批量生产数量")]
        public int TargetBatchCount { get; set; }

        // ============ 灯 / 蜂鸣器 ============

        /// <summary>蓝灯状态（读点 D3115；写点 D3012）：0=灯灭 1=常亮 2=闪烁</summary>
        [PlcRead(PlcDevice.D, "3115", DisplayName = "蓝灯状态")]
        [PlcWrite(PlcDevice.D, "3012", DisplayName = "蓝灯状态")]
        public BlueLampState BlueLamp { get; set; }

        /// <summary>蜂鸣器状态（读点 D3116；写点 D3013）：0=无蜂鸣 1=蜂鸣</summary>
        [PlcRead(PlcDevice.D, "3116", DisplayName = "蜂鸣器状态")]
        [PlcWrite(PlcDevice.D, "3013", DisplayName = "蜂鸣器状态")]
        public BuzzerState Buzzer { get; set; }

        // ============ 只读产量 / 使用次数（浮点） ============

        /// <summary>有效功率（D3895，Float32）</summary>
        [PlcRead(PlcDevice.D, "3895", DisplayName = "有效功率")]
        public float EffectivePower { get; set; }

        /// <summary>總生產量（D215，Float32）</summary>
        [PlcRead(PlcDevice.D, "215", DisplayName = "總生產量")]
        public float TotalProduction { get; set; }

        /// <summary>日生產量（D217，Float16）</summary>
        [PlcRead(PlcDevice.D, "217", DisplayName = "日生產量")]
        public float DailyProduction { get; set; }

        /// <summary>切刀使用次数 PV（D246，只读）</summary>
        [PlcRead(PlcDevice.D, "246", DisplayName = "切刀使用次数")]
        public int CutterUseCount { get; set; }

        /// <summary>一段上压轮使用时间 PV（D249，只读）</summary>
        [PlcRead(PlcDevice.D, "249", DisplayName = "一段上压轮使用时间")]
        public int UpperRollerTimeStage1 { get; set; }

        /// <summary>一段下压轮使用时间 PV（D251，只读）</summary>
        [PlcRead(PlcDevice.D, "251", DisplayName = "一段下压轮使用时间")]
        public int LowerRollerTimeStage1 { get; set; }

        /// <summary>二段上压轮使用时间 PV（D250，只读）</summary>
        [PlcRead(PlcDevice.D, "250", DisplayName = "二段上压轮使用时间")]
        public int UpperRollerTimeStage2 { get; set; }

        /// <summary>二段下压轮使用时间 PV（D252，只读）</summary>
        [PlcRead(PlcDevice.D, "252", DisplayName = "二段下压轮使用时间")]
        public int LowerRollerTimeStage2 { get; set; }

        /// <summary>贴膜压条使用次数 PV（D253，只读）</summary>
        [PlcRead(PlcDevice.D, "253", DisplayName = "贴膜压条使用次数")]
        public int FilmBarUseCount { get; set; }
    }
}
