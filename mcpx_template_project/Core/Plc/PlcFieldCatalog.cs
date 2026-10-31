using System.Reflection;
using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Domain.Enums;

namespace mcpx_template_project.Core.Plc
{
    /// <summary>
    /// 单个字段的 PLC 读点位描述（供界面选择，非数据库实体）。
    /// 由 [PlcRead] 注解扫描而来，描述「哪个类的哪个字段 → 哪个 PLC 地址」。
    /// </summary>
    public sealed class PlcFieldDescriptor
    {
        /// <summary>承载字段的类型</summary>
        public Type OwnerType { get; init; } = null!;

        /// <summary>字段名</summary>
        public string PropertyName { get; init; } = string.Empty;

        /// <summary>设备代号（D/M/X/Y 等）</summary>
        public PlcDevice DeviceCode { get; init; }

        /// <summary>PLC 地址</summary>
        public string Address { get; init; } = string.Empty;

        /// <summary>段内偏移</summary>
        public int Offset { get; init; }

        /// <summary>字段 CLR 类型</summary>
        public Type DataType { get; init; } = typeof(string);

        /// <summary>显示名（未指定时用字段名）</summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>完整点位（设备代号 + 地址）</summary>
        public string FullAddress => $"{DeviceCode}{Address}";
    }

    /// <summary>
    /// PLC 字段目录（缓存）：扫描程序集内所有带 [PlcRead] 注解的字段，
    /// 构建「类型 → 字段列表」两级缓存，供 SECS VID 维护界面下拉选择。
    /// 本目录仅为开发期元数据缓存，不落库；选中后才写入 secs_vid 表。
    /// </summary>
    public sealed class PlcFieldCatalog
    {
        private IReadOnlyList<PlcFieldDescriptor> _fields = Array.Empty<PlcFieldDescriptor>();
        private IReadOnlyDictionary<string, IReadOnlyList<PlcFieldDescriptor>> _byType = new Dictionary<string, IReadOnlyList<PlcFieldDescriptor>>();

        /// <summary>所有带 [PlcRead] 注解的字段（按类型名 + 字段名排序）</summary>
        public IReadOnlyList<PlcFieldDescriptor> Fields => _fields;

        /// <summary>是否已扫描</summary>
        public bool IsScanned { get; private set; }

        /// <summary>
        /// 扫描给定程序集，收集所有带 [PlcRead] 注解的公共实例字段。
        /// </summary>
        public void Scan(Assembly assembly)
        {
            var list = new List<PlcFieldDescriptor>();

            foreach (var type in assembly.GetTypes())
            {
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var attr = prop.GetCustomAttribute<PlcReadAttribute>();
                    if (attr == null) continue;

                    list.Add(new PlcFieldDescriptor
                    {
                        OwnerType = type,
                        PropertyName = prop.Name,
                        DeviceCode = attr.DeviceCode,
                        Address = attr.Address,
                        Offset = attr.Offset,
                        DataType = prop.PropertyType,
                        DisplayName = string.IsNullOrWhiteSpace(attr.DisplayName) ? prop.Name : attr.DisplayName!,
                    });
                }
            }

            _fields = list
                .OrderBy(f => f.OwnerType.Name)
                .ThenBy(f => f.PropertyName)
                .ToList();

            _byType = _fields
                .GroupBy(f => f.OwnerType)
                .ToDictionary(g => g.Key.Name, g => (IReadOnlyList<PlcFieldDescriptor>)g.ToList(), StringComparer.Ordinal);

            IsScanned = true;
        }

        /// <summary>按字段在 <see cref="Fields"/> 中的索引获取描述。</summary>
        public PlcFieldDescriptor? GetByIndex(int index)
        {
            if (index < 0 || index >= _fields.Count) return null;
            return _fields[index];
        }
    }
}
