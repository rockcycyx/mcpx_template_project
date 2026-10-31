using System.Globalization;

namespace mcpx_template_project.Converters.Plc
{
    /// <summary>
    /// 点位值类型转换器：把 PLC 缓存的字符串值（位设备 "ON"/"OFF"，字设备十进制数字）
    /// 转换为强类型业务属性值；并支持反向转换（业务值 → 下发文本）。
    /// </summary>
    public static class PlcValueConverter
    {
        /// <summary>
        /// 把字符串值转换为目标 CLR 类型。
        /// 转换失败时返回目标类型的默认值（宽松策略，避免单个点位异常拖垮整体读取）。
        /// </summary>
        public static object? Convert(string? text, Type targetType)
        {
            var t = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (t == typeof(bool))
                    return ParseBool(text);

                if (t == typeof(int)) return int.Parse(text ?? string.Empty, CultureInfo.InvariantCulture);
                if (t == typeof(long)) return long.Parse(text ?? string.Empty, CultureInfo.InvariantCulture);
                if (t == typeof(short)) return short.Parse(text ?? string.Empty, CultureInfo.InvariantCulture);
                if (t == typeof(ushort)) return ushort.Parse(text ?? string.Empty, CultureInfo.InvariantCulture);
                if (t == typeof(uint)) return uint.Parse(text ?? string.Empty, CultureInfo.InvariantCulture);
                if (t == typeof(byte)) return byte.Parse(text ?? string.Empty, CultureInfo.InvariantCulture);
                if (t == typeof(double)) return double.Parse(text ?? string.Empty, CultureInfo.InvariantCulture);
                if (t == typeof(float)) return float.Parse(text ?? string.Empty, CultureInfo.InvariantCulture);
                if (t == typeof(decimal)) return decimal.Parse(text ?? string.Empty, CultureInfo.InvariantCulture);
                if (t == typeof(string)) return text ?? string.Empty;
                if (t.IsEnum) return Enum.Parse(t, text ?? string.Empty, true);
            }
            catch
            {
                // 转换失败返回默认值，由调用方继续处理其他点位
            }

            return t.IsValueType ? Activator.CreateInstance(t) : null;
        }

        /// <summary>
        /// 把业务属性值转换为下发到 PLC 的文本表示。
        /// </summary>
        public static string ToText(object? value)
        {
            if (value == null) return string.Empty;
            if (value is bool b) return b ? "ON" : "OFF";
            return System.Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        /// <summary>
        /// 解析位设备的 ON/OFF 文本。
        /// 兼容 "ON"/"OFF"/"1"/"0"/"true"/"false" 等常见形式。
        /// </summary>
        private static bool ParseBool(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            var v = text.Trim();
            if (v.Equals("ON", StringComparison.OrdinalIgnoreCase)) return true;
            if (v.Equals("OFF", StringComparison.OrdinalIgnoreCase)) return false;
            if (v == "1") return true;
            if (v == "0") return false;

            return bool.TryParse(v, out var parsed) && parsed;
        }
    }
}
