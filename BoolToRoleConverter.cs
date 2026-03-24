using System;
using System.Globalization;
using System.Windows.Data;

namespace MyPanelCarWashing
{
    public class BoolToRoleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAdmin)
            {
                return isAdmin ? "Администратор" : "Сотрудник";
            }
            return "Сотрудник";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}