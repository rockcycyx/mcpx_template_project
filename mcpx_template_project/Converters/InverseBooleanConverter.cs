using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace mcpx_template_project
{
    /// <summary>
    /// 反转布尔值到Visibility的转换器（true→Collapsed, false→Visible）
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}