using System;
using System.Globalization;
using System.Windows.Data;

namespace mcpx_template_project
{
    /// <summary>
    /// PlcTrigger.Status 字符串（"1"=启用 / "0"=停用）与 bool 之间的转换器。
    /// true = "1"（启用），false = "0"（停用）。
    /// 与全局 StatusToBoolConverter 语义相反（后者 0=正常/1=停用），故单独提供。
    /// </summary>
    public class TriggerStatusToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == "1";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "1" : "0";
        }
    }
}
