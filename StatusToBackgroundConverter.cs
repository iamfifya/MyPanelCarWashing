using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MyPanelCarWashing
{
    public class StatusToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string ?? "В ожидании";

            if (status.Contains("Выполняется"))
                return new SolidColorBrush(Color.FromRgb(52, 152, 219)); // Синий
            if (status.Contains("Выполнен"))
                return new SolidColorBrush(Color.FromRgb(39, 174, 96)); // Зеленый
            if (status.Contains("Отменен"))
                return new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Красный

            return new SolidColorBrush(Color.FromRgb(241, 196, 15)); // Желтый для ожидания
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
