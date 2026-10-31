using mcpx_template_project.Domain.Enums;

namespace mcpx_template_project.Aspect.Attributes
{
    /// <summary>
    /// PLC 写点位注解：声明业务属性下发到哪个 PLC 地址。
    /// 与 <see cref="PlcReadAttribute"/> 分离——读、写各用一个注解。
    /// 通过 <see cref="Role"/> 区分普通值点位与握手位（触发/成功），
    /// 使扫描器能自动分类，业务代码无需按属性名二次判断。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class PlcWriteAttribute : Attribute
    {
        /// <summary>设备代号（枚举，替代字符串）</summary>
        public PlcDevice DeviceCode { get; }

        /// <summary>段起始地址</summary>
        public string Address { get; }

        /// <summary>段内偏移</summary>
        public int Offset { get; }

        /// <summary>中文显示名</summary>
        public string? DisplayName { get; set; }

        /// <summary>点位角色：普通值 / 触发位 / 成功位</summary>
        public PlcWriteRole Role { get; set; } = PlcWriteRole.Value;

        public PlcWriteAttribute(PlcDevice deviceCode, string address, int offset = 0)
        {
            DeviceCode = deviceCode;
            Address = address;
            Offset = offset;
        }
    }
}
