using System;
using System.Globalization;
using System.Windows.Data;

namespace mcpx_template_project.Converters
{
    /// <summary>
    /// 性别值转换器：0→男, 1→女, 其他→未知
    /// </summary>
    public class SexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "0" => "男",
                "1" => "女",
                _ => "未知"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
