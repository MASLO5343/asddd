using System.Windows;
using WpfApp1.ViewModels; // Убедитесь, что это пространство имен корректно для AddEditUserViewModel
using WpfApp1.Interfaces; // Убедитесь, что это пространство имен корректно для IDialogViewModel

namespace WpfApp1.Windows
{
    public partial class AddEditUserWindow : Window
    {
        public AddEditUserWindow(AddEditUserViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Важно, чтобы AddEditUserViewModel реализовывал IDialogViewModel, который определяет CloseRequested
            if (viewModel is IDialogViewModel dialogViewModel)
            {
                // Подписываемся на событие CloseRequested, которое теперь Action<bool?>
                // Лямбда-выражение должно соответствовать этой сигнатуре, принимая один аргумент bool?
                dialogViewModel.CloseRequested += (dialogResult) => // Изменено: (sender, args) на (dialogResult)
                {
                    // dialogResult уже является bool? и this.DialogResult тоже bool?
                    this.DialogResult = dialogResult; // Используем полученный dialogResult
                    this.Close();
                };
            }
            else
            {
                // Логирование ошибки или генерация исключения: viewModel не реализует IDialogViewModel
                // Это может случиться, если AddEditUserViewModel не наследует IDialogViewModel
                // или если используется неправильный интерфейс.
                // Например, можно добавить:
                // throw new InvalidOperationException("ViewModel does not implement IDialogViewModel.");
                // или
                // _logger.LogError("AddEditUserWindow: ViewModel of type {ViewModelType} does not implement IDialogViewModel.", viewModel.GetType().FullName);
            }
        }

        // Пример обработчика нажатия кнопки, который мог вызывать ошибку "DialogResult not in context"
        // Теперь это обычно обрабатывается командой SaveCommand во ViewModel.
        // private void SaveButton_Click(object sender, RoutedEventArgs e)
        // {
        //     // Логика сохранения или валидации...
        //     // Если успешно:
        //     this.DialogResult = true; // Корректно: использовать 'this.DialogResult'
        //     this.Close();
        // }

        // private void CancelButton_Click(object sender, RoutedEventArgs e)
        // {
        //     this.DialogResult = false; // Корректно: использовать 'this.DialogResult'
        //     this.Close();
        // }

        // Относительно "Не удается неявно преобразовать тип "bool?" в "bool"":
        // Эта ошибка возникает, если вы присваиваете bool? (например, this.DialogResult из другого окна)
        // обычной переменной bool без обработки null.
        // Пример:
        // bool someFlag = anotherWindow.DialogResult; // Ошибка, если anotherWindow.DialogResult является bool?
        // Корректно: bool someFlag = anotherWindow.DialogResult == true;
        // Или: bool someFlag = anotherWindow.DialogResult.GetValueOrDefault(false);
        // Этот конкретный файл (AddEditUserWindow.xaml.cs) в основном *устанавливает* DialogResult,
        // поэтому ошибка более вероятна при *чтении* DialogResult в другом месте.
    }
}