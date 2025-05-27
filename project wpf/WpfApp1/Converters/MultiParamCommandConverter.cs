using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input; // For ICommand
using System.Windows.Controls; // For PasswordBox
using CommunityToolkit.Mvvm.Input; // ДОБАВЛЕНО для IRelayCommand

namespace WpfApp1.Converters
{
    public class MultiParamCommandConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0 && values[0] is ICommand command)
            {
                // Создаем список параметров для команды, исключая саму команду
                object[] commandParameters = new object[values.Length - 1];
                Array.Copy(values, 1, commandParameters, 0, values.Length - 1);
                return new MultiCommandWrapper(command, commandParameters);
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MultiCommandWrapper : ICommand
    {
        private readonly ICommand _command;
        private readonly object[] _parameters;

        public event EventHandler? CanExecuteChanged
        {
            add { _command.CanExecuteChanged += value; }
            remove { _command.CanExecuteChanged -= value; }
        }

        public MultiCommandWrapper(ICommand command, object[] parameters)
        {
            _command = command;
            _parameters = parameters;
        }

        public bool CanExecute(object? parameter)
        {
            // Здесь можно добавить логику, если CanExecute команды зависит от параметров
            // или просто передать _parameters напрямую
            // Проверяем, является ли команда IRelayCommand (из CommunityToolkit.Mvvm)
            if (_command is IRelayCommand relayCommand)
            {
                return relayCommand.CanExecute(_parameters);
            }
            return _command.CanExecute(null); // Fallback для других типов ICommand
        }

        public void Execute(object? parameter)
        {
            _command.Execute(_parameters);
        }
    }
}