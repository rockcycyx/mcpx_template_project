using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace mcpx_template_project.Converters
{
    /// <summary>
    /// 整数 → Visibility 转换器：大于 0 → Visible，否则 Collapsed。
    /// 用于警报数角标：警报数为 0 时隐藏角标。
    /// </summary>
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i && i > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
