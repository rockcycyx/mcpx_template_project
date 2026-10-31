namespace mcpx_template_project.Domain.Model.Plc
{
    using mcpx_template_project.Domain.Enums;

    /// <summary>
    /// 读点位定义：描述一个业务属性从哪个 PLC 地址读回当前值。
    /// </summary>
    public class PlcReadDefinition
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
    }
}
