// Converters/StringToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows; // Для Visibility
using System.Windows.Data;

namespace WpfApp1.Converters // <-- CORRECT NAMESPACE
{
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("StringToVisibilityConverter cannot convert back.");
        }
    }
}