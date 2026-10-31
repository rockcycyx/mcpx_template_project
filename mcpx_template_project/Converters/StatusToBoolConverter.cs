using System;
using System.Globalization;
using System.Windows.Data;

namespace mcpx_template_project
{
    /// <summary>
    /// Status 字符串 ("0"/"1") 与 bool 之间的转换器
    /// true = "0" (正常), false = "1" (停用)
    /// </summary>
    public class StatusToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() != "1";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "0" : "1";
        }
    }
}