using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using mcpx_template_project.Helper;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project
{
    /// <summary>
    /// 按钮级权限转换器：ConverterParameter 传权限标识（sys_menu.perms），
    /// 当前用户拥有该权限（或为管理员）→ Visible，否则 Collapsed。
    /// 用法：Visibility="{Binding Converter={StaticResource PermToVisibilityConverter}, ConverterParameter=business:estimation:upload}"
    /// </summary>
    public class PermToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var perm = parameter as string ?? string.Empty;
            var session = AppHelper.GetService<IUserSessionService>();
            return session?.HasPerm(perm) == true ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
