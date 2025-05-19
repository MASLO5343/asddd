using System; // Добавлено для EventArgs в OnClosed, если он останется таким
using System.Windows;
using WpfApp1.Interfaces; // Для IDialogViewModel
// WpfApp1.Services не нужен для DialogCloseRequestedEventArgs в этом сценарии
// WpfApp1.ViewModels также не нужен для прямого использования AddEditDeviceViewModel здесь

namespace WpfApp1.Pages
{
    public partial class AddEditDeviceWindow : Window
    {
        // Сохраняем ссылку на ViewModel для отписки
        private IDialogViewModel? _viewModel;

        public AddEditDeviceWindow(IDialogViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel; // Сохраняем ViewModel
            DataContext = _viewModel;

            // Подписка на событие закрытия из ViewModel
            // viewModel.CloseRequested теперь Action<bool?>
            if (_viewModel != null)
            {
                _viewModel.CloseRequested += ViewModel_CloseRequested;
            }
        }

        // Обработчик события теперь соответствует Action<bool?>
        private void ViewModel_CloseRequested(bool? dialogResult)
        {
            try
            {
                // dialogResult напрямую используется для установки DialogResult окна
                this.DialogResult = dialogResult;
            }
            catch (InvalidOperationException ex)
            {
                // Это может произойти, если окно не было показано как диалоговое.
                // Логирование может быть полезно:
                System.Diagnostics.Debug.WriteLine($"Error setting DialogResult for AddEditDeviceWindow: {ex.Message}. Window might not have been shown as a dialog.");
                // В таком случае просто закрываем.
            }
            Close(); // Закрываем окно
        }

        // Отписка от события при закрытии окна для предотвращения утечек памяти
        protected override void OnClosed(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CloseRequested -= ViewModel_CloseRequested;
            }
            base.OnClosed(e);
        }
    }
}