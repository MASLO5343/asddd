// File: WpfApp1/Converters/CountToVisibilityConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    /// <summary>
    /// Конвертирует число (например, Count коллекции) в Visibility.
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Какое значение Visibility возвращать, если Count == 0. По умолчанию Collapsed.
        /// </summary>
        public Visibility EmptyIs { get; set; } = Visibility.Collapsed;

        /// <summary>
        /// Какое значение Visibility возвращать, если Count > 0. По умолчанию Visible.
        /// </summary>
        public Visibility NonEmptyIs { get; set; } = Visibility.Visible;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? EmptyIs : NonEmptyIs;
            }
            // Можно добавить обработку для других числовых типов или ICollection.Count
            return NonEmptyIs; // По умолчанию считаем не пустым, если тип не int
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}