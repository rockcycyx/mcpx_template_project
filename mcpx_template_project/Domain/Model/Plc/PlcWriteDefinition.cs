using mcpx_template_project.Domain.Enums;

namespace mcpx_template_project.Domain.Model.Plc
{
    /// <summary>
    /// 写点位定义：描述一个业务属性下发到哪个 PLC 地址，及其语义角色。
    /// </summary>
    public class PlcWriteDefinition
    {
        /// <summary>业务属性名</summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>设备代号</summary>
        public PlcDevice DeviceCode { get; set; }

        /// <summary>段起始地址</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>段内偏移</summary>
        public int Offset { get; set; }

        /// <summary>业务属性 CLR 类型</summary>
        public Type DataType { get; set; } = typeof(string);

        /// <summary>中文显示名</summary>
        public string? DisplayName { get; set; }

        /// <summary>点位角色：普通值 / 触发位 / 成功位</summary>
        public PlcWriteRole Role { get; set; } = PlcWriteRole.Value;
    }
}
