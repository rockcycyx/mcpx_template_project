using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace mcpx_template_project.Views.Pages.Admin.Menu
{
    public class BoolToTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b && b ? "修改菜单" : "添加菜单";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
