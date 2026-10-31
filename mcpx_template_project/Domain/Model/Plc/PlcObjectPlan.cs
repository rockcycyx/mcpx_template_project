using System.Collections.Generic;

namespace mcpx_template_project.Domain.Model.Plc
{
    /// <summary>
    /// 某个业务类型 T 的 PLC 读写计划（扫描注解后缓存的结构化结果）。
    /// 把「读点位 / 写值点位 / 触发位 / 回复位」一次分类好，
    /// 供通用读写门面直接消费，业务代码无需任何二次分类。
    /// </summary>
    public class PlcObjectPlan
    {
        /// <summary>读点位定义（读回 PLC 当前值）</summary>
        public IReadOnlyList<PlcReadDefinition> Reads { get; set; } = new List<PlcReadDefinition>();

        /// <summary>写值点位定义（普通业务值下发，Role=Value）</summary>
        public IReadOnlyList<PlcWriteDefinition> WriteValues { get; set; } = new List<PlcWriteDefinition>();

        /// <summary>写入触发位（Role=Trigger），无则 null</summary>
        public PlcWriteDefinition? Trigger { get; set; }

        /// <summary>回复位（Role=Reply），无则 null</summary>
        public PlcWriteDefinition? Reply { get; set; }
    }
}
