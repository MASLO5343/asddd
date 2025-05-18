// File: WpfApp1/Converters/NullToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        // Если значение null, возвращаем Collapsed, иначе Visible
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        // Обратное преобразование не поддерживается/не требуется для этого сценария
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}