using System.Linq.Expressions;
using System.Reflection;
using mcpx_template_project.Core.Scanner;
using mcpx_template_project.Domain.Model.Plc;

namespace mcpx_template_project.Core.Plc
{
    /// <summary>
    /// 业务对象属性访问器：编译一次属性 get 委托，运行期零反射取值。
    /// 供通用写门面在「逐字段下发」时读取业务对象的属性当前值。
    /// </summary>
    public sealed class PlcPointAccessor<T> where T : class
    {
        private readonly IReadOnlyDictionary<string, Func<T, object?>> _getters;

        /// <summary>该类型的读写计划（注解扫描缓存结果）</summary>
        public PlcObjectPlan Plan { get; }

        public PlcPointAccessor()
        {
            Plan = PlcPointAttributeScanner.Scan<T>();
            _getters = BuildGetters(typeof(T));
        }

        /// <summary>读取业务对象某个属性的当前值（供写回下发）。</summary>
        public object? GetValue(T obj, string propertyName)
        {
            return _getters.TryGetValue(propertyName, out var getter)
                ? getter(obj)
                : null;
        }

        private static IReadOnlyDictionary<string, Func<T, object?>> BuildGetters(Type type)
        {
            var dict = new Dictionary<string, Func<T, object?>>(StringComparer.Ordinal);

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                var instance = Expression.Parameter(typeof(T), "instance");
                var access = Expression.Property(instance, prop);
                var cast = Expression.Convert(access, typeof(object));
                dict[prop.Name] = Expression.Lambda<Func<T, object?>>(cast, instance).Compile();
            }

            return dict;
        }
    }
}
