using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace mcpx_template_project
{
    /// <summary>
    /// 字符串为空/null → Visible，非空 → Collapsed 转换器
    /// </summary>
    public class StringToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
                return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}