// fefe-main/WpfApp1/ViewModels/LogsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows; // Для MessageBox (временно)
using WpfApp1.Interfaces; // Для ILogService
using WpfApp1.Models;     // Для модели Log
using System.Linq; // Для OrderByDescending
using WpfApp1.Services.Data;

namespace WpfApp1.ViewModels
{
    // Partial класс необходим для работы Source Generators (ObservableProperty, RelayCommand)
    public partial class LogsViewModel : BaseViewModel // Или ваша базовая ViewModel, если она наследуется от ObservableObject
    {
        private readonly ILogService _logService;
        private readonly ILogger<LogsViewModel> _logger;

        // Коллекция логов для привязки к DataGrid
        [ObservableProperty]
        private ObservableCollection<Log> _logs = new ObservableCollection<Log>();

        // Свойство для индикатора загрузки (опционально, но полезно)
        [ObservableProperty]
        private bool _isLoading = false;

        // Команда для загрузки/обновления логов
        [RelayCommand] // Генерирует public IRelayCommand LoadLogsCommand
        private async Task LoadLogsAsync()
        {
            // Изменение: Добавлен контекст операции и потока
            _logger.LogInformation("LoadLogsAsync: Начата загрузка логов из сервиса.");
            IsLoading = true; // Показываем индикатор
            Logs.Clear(); // Очищаем коллекцию перед загрузкой новых данных

            try
            {
                // Получаем логи из сервиса
                var logsList = await _logService.GetLogsAsync(new LogFilterParameters(), 1, 100);

                // Обновляем ObservableCollection в UI потоке
                // Это важно, если GetAllLogsAsync() не гарантирует возврат в UI поток
                // (асинхронные операции часто выполняются в пуле потоков)
                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (logsList != null)
                        {
                            // Добавляем логи в коллекцию
                            // Можно добавить сортировку, например, по дате убывания
                            foreach (var log in logsList.OrderByDescending(l => l.TimeStamp))
                            {
                                Logs.Add(log);
                            }
                            // Изменение: Добавлен контекст о количестве загруженных записей
                            _logger.LogInformation("LoadLogsAsync: Загружено {LogCount} записей логов и добавлены в коллекцию UI.", Logs.Count);
                        }
                        else
                        {
                            // Изменение: Добавлен контекст, что сервис вернул null при запросе логов
                            _logger.LogWarning("LoadLogsAsync: ILogService.GetAllLogsAsync вернул null. Коллекция логов не обновлена.");
                        }
                    });
                }
                else
                {
                    // Изменение: Улучшено описание ошибки, добавлена причина и последствия
                    _logger.LogError("LoadLogsAsync: Application.Current или Dispatcher недоступен. Не удалось обновить коллекцию логов в UI потоке, так как доступ к диспетчеру UI отсутствует.");
                    // TODO: Возможно, обработать ошибку другим способом, если нет доступа к Dispatcher
                }
            }
            catch (Exception ex)
            {
                // Изменение: Добавлен контекст операции, при которой произошла ошибка
                _logger.LogError(ex, "LoadLogsAsync: Ошибка при загрузке логов из сервиса и обновлении UI.");
                // Показываем пользователю сообщение об ошибке (временно)
                MessageBox.Show($"Ошибка загрузки логов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false; // Скрываем индикатор
                // Изменение: Добавлен контекст завершения операции загрузки
                _logger.LogInformation("LoadLogsAsync: Загрузка логов завершена.");
            }
        }

        // Конструктор ViewModel с внедрением зависимостей
        public LogsViewModel(ILogService logService, ILogger<LogsViewModel> logger)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // Изменение: Добавлен контекст, указывающий, что ViewModel был создан
            _logger.LogInformation("LogsViewModel создан.");

            // Инициируем загрузку логов при создании ViewModel
            // Используем _ = Task.Run(...) или LoadLogsCommand.Execute(null)
            // _ = Task.Run(LoadLogsAsync); // Можно так, если не нужна возможность отмены и т.д.
            LoadLogsCommand.Execute(null); // Вызываем команду загрузки логов
        }

        // TODO: Возможно, добавить команду для обновления списка логов (просто вызывает LoadLogsAsync)
        // [RelayCommand]
        // private async Task RefreshLogsAsync() => await LoadLogsAsync();

        // TODO: Возможно, добавить логику для пагинации, фильтрации или поиска логов
    }
}