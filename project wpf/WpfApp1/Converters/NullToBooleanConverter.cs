// File: WpfApp1/Converters/NullToBooleanConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class NullToBooleanConverter : IValueConverter
    {
        // Если значение null, возвращаем false, иначе true
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        // Обратное преобразование не поддерживается/не требуется для этого сценария
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}