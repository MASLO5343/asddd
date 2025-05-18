// fefe-main/WpfApp1/MainWindow.xaml.cs
// MainWindow.xaml.cs
using Microsoft.Extensions.DependencyInjection; // Для IServiceProvider (хотя если он только для навигации, можно инжектировать INavigationService напрямую)
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using System.Windows.Controls; // Для Frame
using System.Windows.Input;  // Для MouseButtonEventArgs
using WpfApp1.Interfaces;    // Для INavigationService
using WpfApp1.ViewModels;   // Для MainViewModel

namespace WpfApp1;

// Реализуем интерфейс навигации
public partial class MainWindow : Window, INavigationService
{
    private readonly IServiceProvider _serviceProvider; // Нужен для создания страниц
    private readonly ILogger<MainWindow> _logger; // Логгер для самого окна
    private readonly MainViewModel _viewModel; // Ссылка на ViewModel

    // УДАЛИ старые поля, если они были:
    // private readonly IApplicationStateService _appStateService;
    // private readonly IPermissionService _permissionService;

    // Конструктор для использования с DI.
    // Внедряем ViewModel и необходимые сервисы.
    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider, ILogger<MainWindow> logger /*, возможно другие сервисы, если нужны окну*/)
    {
        InitializeComponent();

        // Устанавливаем DataContext окна на инжектированную ViewModel
        DataContext = viewModel;
        _viewModel = viewModel; // Сохраняем ссылку на ViewModel для подписки/отписки

        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // --- ВРЕМЕННО ЗАКОММЕНТИРОВАНО ДЛЯ ДИАГНОСТИКИ ---
        // Подписываемся на событие LogoutRequested из ViewModel.
        // Когда ViewModel решит, что нужно выйти (пользователь нажал кнопку Выход),
        // она вызовет это событие.
         _viewModel.LogoutRequested += ViewModel_LogoutRequested;
        // _logger.LogInformation("MainWindow создан и подписан на LogoutRequested.");
        // ----------------------------------------------------


        // УДАЛИ вызов старых методов:
        // LoadCurrentUser();
        // ApplyPermissions();
    }

    // Обработчик события LogoutRequested из MainViewModel (его код остается, просто подписка временно убрана)
    private void ViewModel_LogoutRequested(object? sender, EventArgs e)
    {
        _logger.LogInformation("MainWindow получил событие LogoutRequested от ViewModel. Закрытие окна...");
        // Отписываемся от события, чтобы избежать утечек памяти
        if (sender is MainViewModel viewModel) // Проверка для безопасности
        {
            viewModel.LogoutRequested -= ViewModel_LogoutRequested;
        }
        // Закрываем окно MainWindow
        this.Close();
        // Приложение App.xaml.cs, которое показало MainWindow через Show(),
        // получит управление обратно и сможет показать LoginWindow.
    }


    // УДАЛИ старые методы, если они были:
    // private void LoadCurrentUser() { ... }
    // private void ApplyPermissions() { ... }
    // private void NavigateToPage<TPage>() where TPage : Page { ... }
    // УДАЛИ все обработчики кликов навигационных кнопок (InventoryButton_Click и т.д.)
    // private void InventoryButton_Click(object sender, RoutedEventArgs e) { ... }
    // private void TicketsButton_Click(object sender, RoutedEventArgs e) { ... }
    // ... и так далее
    // УДАЛИ обработчик LogoutButton_Click (если он был)
    // private void LogoutButton_Click(object sender, RoutedEventArgs e) { ... }


    // --- РЕАЛИЗАЦИЯ INavigationService ---
    // Этот метод вызывается из ViewModel (через _navigationService)
    public void NavigateTo(Type pageType)
    {
        try
        {
            // Проверяем, что MainFrame существует (имя из XAML)
            // Убедитесь, что у Вас есть <Frame x:Name="MainFrame"/> в MainWindow.xaml
            if (MainFrame != null) // MainFrame - это поле, сгенерированное XAML-компилятором по x:Name
            {
                // Получаем экземпляр страницы из DI контейнера
                // Используем _serviceProvider, который инжектирован в MainWindow
                var page = _serviceProvider.GetRequiredService(pageType);

                _logger.LogInformation("Navigating MainFrame to {PageType}", pageType.Name);
                // Выполняем навигацию во Frame
                MainFrame.Navigate(page);
            }
            else
            {
                _logger.LogError("MainFrame is null. Cannot navigate to {PageType}.", pageType.Name);
                // TODO: Уведомить пользователя (возможно, через сервис сообщений)
                // MessageBox.Show($"Ошибка навигации: Не найден Frame для загрузки страницы {pageType.Name}.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during navigation to {PageType}.", pageType.Name);
            MessageBox.Show($"Ошибка при переходе на страницу {pageType.Name}: {ex.Message}", "Ошибка навигации", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    // --- КОНЕЦ РЕАЛИЗАЦИИ INavigationService ---


    // ОСТАВЬ обработчики для перемещения окна, если используешь кастомный заголовок
    private void WindowHeader_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove(); // Стандартный метод Window для перемещения
    }

    // ОСТАВЬ обработчик закрытия, если он есть (например, для кнопки закрытия в рамке окна)
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Закрытие окна. Логика завершения приложения или возврата к логину
        // должна обрабатываться на более высоком уровне (App.xaml.cs),
        // которое показало это окно.
        this.Close();
        // Application.Current.Shutdown(); // Это завершит всё приложение, возможно, это нужно делать в другом месте
    }

    // ОСТАВЬ обработчик Window_Closing, если он нужен (например, для подтверждения выхода при закрытии окна)
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Логирование или другая логика перед закрытием окна
        _logger?.LogInformation("MainWindow is closing.");
    }

    // TODO: Реализовать отписку от событий, если MainWindow может быть закрыт и пересоздан
    // Если MainWindow - Singleton (как в Вашем App.xaml.cs), то отписка от LogoutRequested
    // в ViewModel_LogoutRequested достаточна, т.к. окно не будет пересоздаваться.
    // Если MainWindow может быть закрыт и открыт заново (не синглтон),
    // то нужно отписываться от событий сервисов в MainViewModel.Cleanup() и вызывать Cleanup()
    // при закрытии окна (например, в обработчике Window_Closed).
}