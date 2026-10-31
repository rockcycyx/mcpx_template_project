using mcpx_template_project.Domain.Enums;

namespace mcpx_template_project.Aspect.Attributes
{
    /// <summary>
    /// PLC 读点位注解：声明业务属性从哪个 PLC 地址读回当前值。
    /// 与 <see cref="PlcWriteAttribute"/> 分离——读、写各用一个注解，语义清晰。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class PlcReadAttribute : Attribute
    {
        /// <summary>设备代号（枚举，替代字符串）</summary>
        public PlcDevice DeviceCode { get; }

        /// <summary>段起始地址</summary>
        public string Address { get; }

        /// <summary>段内偏移</summary>
        public int Offset { get; }

        /// <summary>中文显示名</summary>
        public string? DisplayName { get; set; }

        public PlcReadAttribute(PlcDevice deviceCode, string address, int offset = 0)
        {
            DeviceCode = deviceCode;
            Address = address;
            Offset = offset;
        }
    }
}
