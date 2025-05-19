using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfApp1.Commands;
using WpfApp1.Constants; // Для Permissions
using WpfApp1.Interfaces;
using WpfApp1.Models;
using Serilog.Context;
using System.Linq; // Добавлено для FirstOrDefault в NavigateToPage (если потребуется)


namespace WpfApp1.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ILogger<MainViewModel> _logger;
        // Убираем прямые зависимости из конструктора, если они устанавливаются как свойства из App.xaml.cs
        // или внедряются через ServiceProvider, если такой подход выбран.
        // Однако, лучше явное внедрение через конструктор, если сервисы используются сразу.
        // Для NavigationService, PermissionService, ApplicationStateService, они устанавливаются как свойства из App.xaml.cs

        private User? _currentUser; // Сделаем User nullable, т.к. пользователь может быть не авторизован
        public User? CurrentUser // Тип User? (nullable)
        {
            get => _currentUser;
            set
            {
                if (SetProperty(ref _currentUser, value)) // Используем SetProperty для User?
                {
                    IsUserLoggedIn = _currentUser != null;
                    // Update command CanExecute if needed
                    // Приведение к RelayCommand может быть небезопасным, если команды инициализируются позже или другим типом.
                    // Лучше проверять на null перед вызовом RaiseCanExecuteChanged.
                    (NavigateToDashboardCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (NavigateToInventoryCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (NavigateToUsersCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (NavigateToTicketsCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (NavigateToLogsCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (NavigateToMonitoringCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (LogoutCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isUserLoggedIn;
        public bool IsUserLoggedIn
        {
            get => _isUserLoggedIn;
            private set => SetProperty(ref _isUserLoggedIn, value); // Сделаем set приватным, т.к. он зависит от CurrentUser
        }

        public INavigationService? NavigationService { get; set; } // Сделаем сервисы nullable, если они устанавливаются позже
        public IPermissionService? PermissionService { get; set; }
        public IApplicationStateService? ApplicationStateService { get; set; }
        public IDialogService? DialogService { get; set; }
        public IServiceProvider? ServiceProvider { get; set; }



        public ICommand? NavigateToDashboardCommand { get; private set; } // Команды могут быть null до инициализации
        public ICommand? NavigateToInventoryCommand { get; private set; }
        public ICommand? NavigateToUsersCommand { get; private set; }
        public ICommand? NavigateToTicketsCommand { get; private set; }
        public ICommand? NavigateToLogsCommand { get; private set; }
        public ICommand? NavigateToMonitoringCommand { get; private set; }
        public ICommand? LogoutCommand { get; private set; } // Команда выхода

        public event EventHandler? LogoutRequested; // Событие выхода пользователя

        // Конструктор должен принимать ILogger, остальные сервисы устанавливаются как свойства
        public MainViewModel(ILogger<MainViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // ApplicationStateService, PermissionService, NavigationService, DialogService, ServiceProvider
            // будут установлены из App.xaml.cs перед вызовом InitializeAsync()
            _logger.LogInformation("Экземпляр MainViewModel создан (без инициализации сервисов).");
        }

        public async Task InitializeAsync()
        {
            // Проверки на null для всех используемых сервисов
            if (ApplicationStateService == null) throw new InvalidOperationException($"{nameof(ApplicationStateService)} не установлен. Вызовите его установку перед InitializeAsync.");
            if (PermissionService == null) throw new InvalidOperationException($"{nameof(PermissionService)} не установлен.");
            if (NavigationService == null) throw new InvalidOperationException($"{nameof(NavigationService)} не установлен.");
            if (DialogService == null) throw new InvalidOperationException($"{nameof(DialogService)} не установлен.");
            if (ServiceProvider == null) throw new InvalidOperationException($"{nameof(ServiceProvider)} не установлен.");

            // Подписываемся на событие изменения текущего пользователя
            ApplicationStateService.CurrentUserChanged += OnCurrentUserChanged;
            CurrentUser = ApplicationStateService.CurrentUser; // Установить текущего пользователя при инициализации

            // Использование LogContext для добавления свойств ко всем логам в этом блоке
            using (LogContext.PushProperty("UserId", CurrentUser?.UserId.ToString() ?? "null")) // Убедимся что UserId это строка
            using (LogContext.PushProperty("UserName", CurrentUser?.Username ?? "N/A"))
            {
                _logger.LogInformation("MainViewModel инициализируется для пользователя: {Username}", CurrentUser?.Username ?? "N/A");

                InitializeNavigationCommands(); // Инициализация навигационных команд
                InitializeDefaultNavigation();  // Навигация на страницу по умолчанию

                // Инициализация команды выхода
                LogoutCommand = new RelayCommand(async _ => await ExecuteLogoutAsync(), _ => IsUserLoggedIn);

                // Можно добавить загрузку каких-либо данных, если требуется (await LoadInitialDataAsync();)
                _logger.LogInformation("MainViewModel успешно инициализирован.");
            }
            // Здесь можно убрать await Task.CompletedTask; если метод действительно асинхронный
            // Если нет await операций, можно сделать метод синхронным void Initialize()
            // Но так как команды могут вызывать async методы, оставим Task
            await Task.CompletedTask; // Если нет других await, но метод должен быть async Task
        }

        // Обработчик изменения текущего пользователя
        private void OnCurrentUserChanged(object? sender, EventArgs e) // Стандартный EventHandler
        {
            // Получаем нового пользователя из сервиса состояния
            User? newUser = ApplicationStateService?.CurrentUser;
            _logger.LogDebug("CurrentUser изменился в MainViewModel. Новый пользователь: {Username}", newUser?.Username ?? "N/A");
            CurrentUser = newUser; // Обновляем свойство CurrentUser, что вызовет обновление UI и команд
        }

        // Инициализация навигационных команд
        private void InitializeNavigationCommands()
        {
            _logger.LogDebug("Инициализация навигационных команд.");
            NavigateToDashboardCommand = new RelayCommand(_ => NavigateToPage<DashboardViewModel>(), _ => CanNavigateToPage(Permissions.Pages.DashboardView));
            NavigateToInventoryCommand = new RelayCommand(_ => NavigateToPage<InventoryViewModel>(), _ => CanNavigateToPage(Permissions.Pages.InventoryView));
            NavigateToUsersCommand = new RelayCommand(_ => NavigateToPage<UsersViewModel>(), _ => CanNavigateToPage(Permissions.Pages.UsersView));
            NavigateToTicketsCommand = new RelayCommand(_ => NavigateToPage<TicketsViewModel>(), _ => CanNavigateToPage(Permissions.Pages.TicketsView));
            NavigateToLogsCommand = new RelayCommand(_ => NavigateToPage<LogsViewModel>(), _ => CanNavigateToPage(Permissions.Pages.LogsView));
            NavigateToMonitoringCommand = new RelayCommand(_ => NavigateToPage<MonitoringViewModel>(), _ => CanNavigateToPage(Permissions.Pages.MonitoringView));
        }

        // Навигация на указанную ViewModel/Page
        private void NavigateToPage<TViewModel>() where TViewModel : BaseViewModel
        {
            if (NavigationService == null)
            {
                _logger.LogError("NavigationService не установлен при попытке навигации на {ViewModelName}", typeof(TViewModel).Name);
                DialogService?.ShowError("Ошибка навигации", "Сервис навигации не доступен.", "Обратитесь к администратору.");
                return;
            }
            try
            {
                // Логика определения типа страницы остается такой же, как у вас
                Type? pageTypeToNavigate = null;
                string viewModelName = typeof(TViewModel).Name;
                string expectedPageName = viewModelName.Replace("ViewModel", "Page");

                pageTypeToNavigate = Type.GetType($"WpfApp1.Pages.{expectedPageName}") ?? Type.GetType($"WpfApp1.Views.{expectedPageName}");

                if (pageTypeToNavigate == null)
                {
                    var assembly = typeof(App).Assembly;
                    pageTypeToNavigate = assembly.GetTypes().FirstOrDefault(t =>
                        (t.Namespace == "WpfApp1.Pages" || t.Namespace == "WpfApp1.Views") &&
                        t.Name == expectedPageName &&
                        (typeof(System.Windows.Controls.Page).IsAssignableFrom(t) || typeof(System.Windows.Controls.UserControl).IsAssignableFrom(t))
                    );
                }

                if (pageTypeToNavigate != null)
                {
                    _logger.LogInformation("MainViewModel: Навигация на тип страницы: {PageType} для ViewModel: {ViewModelName}", pageTypeToNavigate.FullName, viewModelName);
                    NavigationService.NavigateTo(pageTypeToNavigate);
                }
                else
                {
                    _logger.LogError("MainViewModel: Не удалось определить тип страницы для ViewModel: {ViewModelName}. Ожидалось имя: {ExpectedPageName}", viewModelName, expectedPageName);
                    DialogService?.ShowError("Ошибка навигации", $"Не удается найти страницу для {viewModelName}.", "");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MainViewModel: Ошибка при навигации на страницу для ViewModel {ViewModelName}", typeof(TViewModel).Name);
                DialogService?.ShowError("Ошибка навигации", $"Не удалось перейти на страницу для {typeof(TViewModel).Name}.", ex.Message);
            }
        }

        // Проверка прав на навигацию
        private bool CanNavigateToPage(string pagePermissionKey)
        {
            if (CurrentUser == null || CurrentUser.Role == null || PermissionService == null)
            {
                //_logger.LogWarning("Невозможно проверить права навигации: CurrentUser, Role или PermissionService не установлены.");
                return false;
            }
            bool hasPermission = PermissionService.HasPermission(CurrentUser.Role.RoleName, pagePermissionKey);
            //_logger.LogTrace("Проверка прав для '{Username}' на '{Permission}': {HasPermission}", CurrentUser.Username, pagePermissionKey, hasPermission);
            return hasPermission;
        }

        // Навигация на страницу по умолчанию при инициализации
        private void InitializeDefaultNavigation()
        {
            if (NavigationService == null)
            {
                _logger.LogError("NavigationService не установлен в MainViewModel перед InitializeDefaultNavigation.");
                return;
            }
            _logger.LogDebug("Инициализация навигации по умолчанию.");
            // Логика выбора страницы по умолчанию остается такой же
            if (CanNavigateToPage(Permissions.Pages.DashboardView)) NavigateToPage<DashboardViewModel>();
            else if (CanNavigateToPage(Permissions.Pages.InventoryView)) NavigateToPage<InventoryViewModel>();
            else if (CanNavigateToPage(Permissions.Pages.TicketsView)) NavigateToPage<TicketsViewModel>();
            else if (CanNavigateToPage(Permissions.Pages.UsersView)) NavigateToPage<UsersViewModel>();
            else if (CanNavigateToPage(Permissions.Pages.LogsView)) NavigateToPage<LogsViewModel>();
            else if (CanNavigateToPage(Permissions.Pages.MonitoringView)) NavigateToPage<MonitoringViewModel>();
            else
            {
                _logger.LogWarning("У пользователя {Username} нет прав для навигации на какую-либо страницу по умолчанию.", CurrentUser?.Username ?? "N/A");
                DialogService?.ShowMessage("Навигация", "У вас нет доступа к страницам по умолчанию. Обратитесь к администратору.");
            }
        }

        // --- ИЗМЕНЕННЫЙ МЕТОД ВЫХОДА ---
        private async Task ExecuteLogoutAsync()
        {
            _logger.LogInformation("Пользователь {Username} инициировал выход из системы.", CurrentUser?.Username ?? "N/A");

            if (DialogService == null)
            {
                _logger.LogError($"{nameof(DialogService)} не установлен. Невозможно подтвердить выход.");
                // В крайнем случае, если выход без подтверждения допустим:
                // ApplicationStateService?.ClearCurrentUser();
                // LogoutRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            bool? confirmed = await DialogService.ShowConfirmAsync("Выход", "Вы уверены, что хотите выйти из системы?");
            if (confirmed == true)
            {
                _logger.LogInformation("Выход подтвержден пользователем {Username}.", CurrentUser?.Username ?? "N/A");
                if (ApplicationStateService == null)
                {
                    _logger.LogError($"{nameof(ApplicationStateService)} не установлен. Невозможно очистить текущего пользователя.");
                    return;
                }
                ApplicationStateService.ClearCurrentUser(); // Очистка состояния текущего пользователя

                _logger.LogInformation("Состояние пользователя очищено. Вызов события LogoutRequested.");
                // Вызываем событие, чтобы MainWindow мог закрыться
                LogoutRequested?.Invoke(this, EventArgs.Empty);

                // Старая логика прямого завершения приложения (ЗАКОММЕНТИРОВАНА/УДАЛЕНА):
                // _logger.LogInformation("Перезапуск приложения после выхода пользователя.");
                // var currentApp = (App)System.Windows.Application.Current;
                // currentApp.RestartApplication(); // Нужен метод для перезапуска в App.xaml.cs
                // System.Windows.Application.Current.Shutdown();
            }
            else
            {
                _logger.LogInformation("Выход из системы отменен пользователем {Username}.", CurrentUser?.Username ?? "N/A");
            }
        }
        // --- КОНЕЦ ИЗМЕНЕННОГО МЕТОДА ВЫХОДА ---

        // Метод для очистки ресурсов, например, отписка от событий
        public void Cleanup()
        {
            _logger.LogInformation("MainViewModel Cleanup вызывается.");
            if (ApplicationStateService != null)
            {
                ApplicationStateService.CurrentUserChanged -= OnCurrentUserChanged;
                _logger.LogDebug("Отписка от ApplicationStateService.CurrentUserChanged выполнена.");
            }
            else
            {
                _logger.LogWarning("ApplicationStateService был null во время Cleanup, отписка не выполнена.");
            }
            _logger.LogInformation("MainViewModel очищен.");
        }
    }
}