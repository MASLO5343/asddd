using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.Interfaces;
using WpfApp1.Views.Dialogs; // Для DialogWindowBase

namespace WpfApp1.Services
{
    public class DialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DialogService> _logger;

        public DialogService(IServiceProvider serviceProvider, ILogger<DialogService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void ShowMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async Task ShowMessageAsync(string title, string message)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information)
            );
        }

        public void ShowError(string title, string message, string details = "")
        {
            _logger.LogError("Ошибка отображена пользователю: {ErrorTitle} - {ErrorMessage}. Детали: {ErrorDetails}", title, message, details);
            // Можно использовать кастомное окно для отображения ошибок с деталями
            string fullMessage = string.IsNullOrWhiteSpace(details) ? message : $"{message}\n\nДетали:\n{details}";
            MessageBox.Show(fullMessage, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        public async Task ShowErrorAsync(string title, string message, string details = "")
        {
            _logger.LogError("Асинхронная ошибка отображена пользователю: {ErrorTitle} - {ErrorMessage}. Детали: {ErrorDetails}", title, message, details);
            string fullMessage = string.IsNullOrWhiteSpace(details) ? message : $"{message}\n\nДетали:\n{details}";
            await Application.Current.Dispatcher.InvokeAsync(() =>
                 MessageBox.Show(fullMessage, title, MessageBoxButton.OK, MessageBoxImage.Error)
            );
        }


        public bool ShowConfirm(string title, string message)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
        public async Task<bool> ShowConfirmAsync(string title, string message)
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
            );
        }

        public async Task<bool?> ShowDialogAsync<TViewModel>(TViewModel viewModel, string title)
            where TViewModel : IDialogViewModel
        {
            _logger.LogDebug("Открытие диалога (bool?) типа {ViewModelType} с заголовком: {DialogTitle}", typeof(TViewModel).Name, title);
            var tcs = new TaskCompletionSource<bool?>();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                viewModel.Title = title;
                var dialogWindow = _serviceProvider.GetRequiredService<DialogWindowBase>();
                dialogWindow.DataContext = viewModel;
                dialogWindow.Owner = Application.Current.MainWindow;

                Action<bool?> closeHandler = null;
                closeHandler = (result) =>
                {
                    viewModel.CloseRequested -= closeHandler; // Отписаться
                    dialogWindow.DialogResult = result; // Для стандартного закрытия окна WPF
                    dialogWindow.Close();
                    tcs.TrySetResult(result);
                    _logger.LogDebug("Диалог (bool?) {ViewModelType} закрыт с результатом: {DialogResult}", typeof(TViewModel).Name, result);
                };
                viewModel.CloseRequested += closeHandler;

                dialogWindow.ShowDialog(); // Блокирующий вызов на UI потоке

                // Если окно было закрыто не через RequestClose (например, крестиком),
                // а событие не успело отработать или tcs не установлен.
                if (!tcs.Task.IsCompleted)
                {
                    // DialogWindow.DialogResult может быть null, если закрыто крестиком
                    // или если не было установлено в true/false до закрытия.
                    tcs.TrySetResult(dialogWindow.DialogResult);
                    _logger.LogDebug("Диалог (bool?) {ViewModelType} закрыт стандартным механизмом WPF с результатом: {DialogResult}", typeof(TViewModel).Name, dialogWindow.DialogResult);
                }
            });
            return await tcs.Task;
        }
        public void ShowWarning(string title, string message)
        {
            _logger.LogWarning("Предупреждение отображено пользователю: {WarningTitle} - {WarningMessage}", title, message);
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public async Task ShowWarningAsync(string title, string message)
        {
            _logger.LogWarning("Асинхронное предупреждение отображено пользователю: {WarningTitle} - {WarningMessage}", title, message);
            await Application.Current.Dispatcher.InvokeAsync(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning)
            );
        }


        public async Task<TDialogResult> ShowCustomDialogAsync<TViewModel, TDialogResult>(TViewModel viewModel, string title)
           where TViewModel : IDialogViewModel<TDialogResult>
        {
            _logger.LogDebug("Открытие кастомного диалога типа {ViewModelType} с заголовком: {DialogTitle}", typeof(TViewModel).Name, title);
            var tcs = new TaskCompletionSource<TDialogResult>();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                viewModel.Title = title;
                // Предполагаем, что DialogWindowBase может хостить любой ViewModel.
                // Для разных типов диалогов можно использовать разные базовые окна, если требуется.
                var dialogWindow = _serviceProvider.GetRequiredService<DialogWindowBase>();
                dialogWindow.DataContext = viewModel;
                dialogWindow.Owner = Application.Current.MainWindow;

                Action<TDialogResult> closeWithResultHandler = null;
                closeWithResultHandler = (customResult) =>
                {
                    viewModel.CloseRequestedWithResult -= closeWithResultHandler;
                    // Установка DialogResult для окна может быть опциональной, если результат передается через TDialogResult
                    // dialogWindow.DialogResult = customResult != null; // Зависит от того, как null TDialogResult интерпретируется
                    dialogWindow.Close();
                    tcs.TrySetResult(customResult);
                    _logger.LogDebug("Кастомный диалог {ViewModelType} закрыт с результатом: {@DialogResult}", typeof(TViewModel).Name, customResult);
                };
                viewModel.CloseRequestedWithResult += closeWithResultHandler;

                // Обработка простого закрытия (OK/Cancel), если ViewModel также реализует это
                Action<bool?> simpleCloseHandler = null;
                simpleCloseHandler = (simpleResult) => {
                    viewModel.CloseRequested -= simpleCloseHandler;
                    if (!tcs.Task.IsCompleted) // Если кастомный результат еще не был установлен
                    {
                        dialogWindow.DialogResult = simpleResult;
                        dialogWindow.Close();
                        // Если простое закрытие означает "нет результата" для кастомного диалога
                        tcs.TrySetResult(default(TDialogResult));
                        _logger.LogDebug("Кастомный диалог {ViewModelType} закрыт через простой механизм с результатом: {SimpleResult}, кастомный результат установлен в default.", typeof(TViewModel).Name, simpleResult);
                    }
                };
                if (viewModel is IDialogViewModel nonGenericVm) // Проверяем, реализует ли также базовый интерфейс
                {
                    nonGenericVm.CloseRequested += simpleCloseHandler;
                }


                dialogWindow.ShowDialog(); // Блокирующий вызов

                if (!tcs.Task.IsCompleted)
                {
                    // Если закрыто крестиком, возвращаем default (null для ссылочных типов)
                    tcs.TrySetResult(default(TDialogResult));
                    _logger.LogDebug("Кастомный диалог {ViewModelType} закрыт стандартным механизмом WPF без установки результата (вероятно, крестик).", typeof(TViewModel).Name);
                }
            });

            return await tcs.Task;
        }
    }
}