// WpfApp1/Converters/BooleanToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp1.Converters // <--- ДОБАВЛЕНО ПРОСТРАНСТВО ИМЕН
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Оригинальный ConvertBack вызывал NotImplementedException.
            // Для BooleanToVisibilityConverter часто используют такой вариант,
            // хотя он не всегда идеально обратим.
            if (value is Visibility visibilityValue)
            {
                return visibilityValue == Visibility.Visible;
            }
            // Или можно оставить NotImplementedException, если обратное преобразование не нужно.
            throw new NotSupportedException("BooleanToVisibilityConverter cannot convert back in this implementation.");
        }
    }
}