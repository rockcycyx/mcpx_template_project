using System;
using System.Globalization;
using System.Windows.Data;

namespace mcpx_template_project
{
    /// <summary>
    /// int 与 string 互转：0 显示为空字符串，空/非法输入回退为 0。
    /// 用于 TextBox 绑定 int 属性，避免输入框清空时 ConvertBack 报
    /// "cannot convert value ''" 的绑定错误。
    /// </summary>
    public class IntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int i && i != 0 ? i.ToString() : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is string s && int.TryParse(s.Trim(), out var result) ? result : 0;
        }
    }
}
