using System;
using System.Windows.Input;

namespace WpfApp1.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute; // Изменено на Action<object?>
        private readonly Predicate<object?>? _canExecute; // Изменено на Predicate<object?>?

        public event EventHandler? CanExecuteChanged // Nullable event handler
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Конструктор для команд с параметрами
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Перегрузка конструктора для команд без параметров для удобства
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(param => execute(), param => canExecute == null || canExecute())
        {
            // Вызов 'this' выше сопоставляет execute/canExecute без параметров
            // с основным конструктором, который ожидает параметры.
        }

        public bool CanExecute(object? parameter) // Параметр теперь object?
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter) // Параметр теперь object?
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}