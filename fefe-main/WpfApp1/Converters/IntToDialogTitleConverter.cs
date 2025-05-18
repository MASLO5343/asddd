// File: WpfApp1/Converters/IntToDialogTitleConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp1.Converters
{
    public class IntToDialogTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string prefix = parameter as string ?? "Запись";
            if (value is int id && id > 0)
            {
                // Редактирование существующей записи
                return $"{prefix} (ID: {id}) - Редактирование";
            }
            else
            {
                // Создание новой записи
                return $"{prefix} - Создание";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}