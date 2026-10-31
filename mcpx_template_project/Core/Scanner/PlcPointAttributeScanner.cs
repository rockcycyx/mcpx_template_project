using System.Collections.Concurrent;
using System.Reflection;
using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Domain.Enums;
using mcpx_template_project.Domain.Model.Plc;

namespace mcpx_template_project.Core.Scanner
{
    /// <summary>
    /// 注解扫描器：从业务类（含继承子类）提取 [PlcRead]/[PlcWrite] 注解，
    /// 一次性分类为 <see cref="PlcObjectPlan"/>（读点/写值点/触发位/回复位）。
    /// 扫描结果按 Type 缓存。
    /// </summary>
    public static class PlcPointAttributeScanner
    {
        private static readonly ConcurrentDictionary<Type, PlcObjectPlan> _cache = new();

        /// <summary>扫描泛型类型 T 的注解点位计划。</summary>
        public static PlcObjectPlan Scan<T>() => Scan(typeof(T));

        /// <summary>扫描指定类型的注解点位计划（含继承链上的属性）。</summary>
        public static PlcObjectPlan Scan(Type type)
        {
            return _cache.GetOrAdd(type, t => ScanCore(t));
        }

        private static PlcObjectPlan ScanCore(Type type)
        {
            var reads = new List<PlcReadDefinition>();
            var writes = new List<PlcWriteDefinition>();

            // 遍历类型的公共实例属性，分别收集读/写注解。
            // 同名属性去重：子类 override 父类属性时保留最派生声明。
            var readSeen = new HashSet<string>(StringComparer.Ordinal);
            var writeSeen = new HashSet<string>(StringComparer.Ordinal);

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                var readAttr = prop.GetCustomAttribute<PlcReadAttribute>();
                if (readAttr != null && readSeen.Add(prop.Name))
                {
                    reads.Add(new PlcReadDefinition
                    {
                        PropertyName = prop.Name,
                        DeviceCode = readAttr.DeviceCode,
                        Address = readAttr.Address,
                        Offset = readAttr.Offset,
                        DataType = prop.PropertyType,
                        DisplayName = readAttr.DisplayName
                    });
                }

                var writeAttr = prop.GetCustomAttribute<PlcWriteAttribute>();
                if (writeAttr != null && writeSeen.Add(prop.Name))
                {
                    writes.Add(new PlcWriteDefinition
                    {
                        PropertyName = prop.Name,
                        DeviceCode = writeAttr.DeviceCode,
                        Address = writeAttr.Address,
                        Offset = writeAttr.Offset,
                        DataType = prop.PropertyType,
                        DisplayName = writeAttr.DisplayName,
                        Role = writeAttr.Role
                    });
                }
            }

            return new PlcObjectPlan
            {
                Reads = reads,
                WriteValues = writes.Where(w => w.Role == PlcWriteRole.Value).ToList(),
                Trigger = writes.FirstOrDefault(w => w.Role == PlcWriteRole.Trigger),
                Reply = writes.FirstOrDefault(w => w.Role == PlcWriteRole.Reply)
            };
        }
    }
}
