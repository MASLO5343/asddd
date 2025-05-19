// WpfApp1/App.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core; // Для LoggingLevelSwitch
using Serilog.Events; // Для LogEventLevel
using System;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.Interfaces;
using WpfApp1.Models;
using WpfApp1.Pages;
using WpfApp1.Services;
using WpfApp1.ViewModels;
using WpfApp1.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using WpfApp1.Views.Dialogs;

namespace WpfApp1
{
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }
        private static LoggingLevelSwitch _loggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information); // Уровень по умолчанию

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    // Конфигурация appsettings.json и UserSecrets уже обрабатывается CreateDefaultBuilder()
                    // Можно добавить дополнительные источники конфигурации здесь, если нужно
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Инициализация Serilog из конфигурации
                    Serilog.Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(hostContext.Configuration)
                        .MinimumLevel.ControlledBy(_loggingLevelSwitch) // Управление уровнем логирования
                        .Enrich.FromLogContext()
                        // Дополнительные Enrichers можно добавить здесь или в appsettings.json
                        .CreateLogger();

                    services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
                    services.AddSingleton(_loggingLevelSwitch); // Для динамического изменения уровня логирования

                    // Регистрация AppDbContext
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection")), // ИСПРАВЛЕНО: Использовать "DefaultConnection"
                        ServiceLifetime.Transient); // Используйте Transient или Scoped в зависимости от потребностей

                    // Регистрация сервисов
                    services.AddSingleton<IApplicationStateService, ApplicationStateService>();
                    services.AddSingleton<IDialogService, DialogService>();

                    // INavigationService теперь регистрируется через MainWindow, которое его реализует
                    // services.AddSingleton<INavigationService, NavigationService>(); // Старая регистрация
                    services.AddSingleton<INavigationService>(provider => provider.GetRequiredService<MainWindow>());


                    services.AddSingleton<IPermissionService, PermissionService>();

                    // IPasswordHasher<User> будет разрешен по умолчанию, если установлен пакет Microsoft.AspNetCore.Identity.Core
                    // services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>(); // Явная регистрация, если нужна специфическая конфигурация или если авто-разрешение не работает

                    services.AddSingleton<IAuthService, AuthService>(); // AuthService теперь синглтон, если нет состояния, которое мешает этому
                    services.AddTransient<IAdAuthService, AdAuthService>(); // Если AdAuthService не имеет состояния
                    services.AddTransient<IUserService, UserService>();
                    services.AddTransient<IRoleService, RoleService>();
                    services.AddTransient<IDeviceService, DeviceService>();
                    services.AddTransient<ISoftwareService, SoftwareService>();
                    services.AddTransient<IDeviceSoftwareService, DeviceSoftwareService>();
                    services.AddTransient<ITicketService, TicketService>();
                    services.AddTransient<ITicketCommentService, TicketCommentService>();
                    services.AddTransient<ILogService, LogService>(); // Для чтения логов из БД
                    services.AddTransient<ILoggingService, LoggingService>(); // Ваш собственный сервис-обертка для логгирования
                    services.AddTransient<IDataSeeder, DatabaseSeeder>();

                    // Регистрация ViewModel'ов
                    services.AddSingleton<MainViewModel>(); // MainViewModel часто делают Singleton
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<InventoryViewModel>();
                    services.AddTransient<AddEditDeviceViewModel>();
                    services.AddTransient<TicketsViewModel>();
                    services.AddTransient<UsersViewModel>();
                    services.AddTransient<AddEditUserViewModel>();
                    services.AddTransient<LogsViewModel>();
                    services.AddTransient<MonitoringViewModel>();
                    services.AddTransient<ViewModels.Dialogs.AddEditTicketDialogViewModel>(); // Уточните пространство имен, если есть конфликт
                    // services.AddTransient<ViewModels.Dialogs.AddSoftwareToDeviceDialogViewModel>(); // Если используется

                    // Регистрация Окон
                    services.AddSingleton<MainWindow>(); // MainWindow часто Singleton
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<AddEditUserWindow>();
                    services.AddTransient<AddEditDeviceWindow>();
                    services.AddTransient<DialogWindowBase>(); // Базовое окно для диалогов

                    // Регистрация Страниц
                    services.AddTransient<DashboardPage>();
                    services.AddTransient<InventoryPage>();
                    services.AddTransient<TicketsPage>();
                    services.AddTransient<UsersPage>();
                    services.AddTransient<LogsPage>();
                    services.AddTransient<MonitoringPage>();
                    // services.AddTransient<ColumnSettingsPage>(); // Если используется
                })
                .Build();

            // Глобальные обработчики исключений
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost!.StartAsync();

            // Сидинг данных и настройка уровня логирования
            using (var scope = AppHost.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
                await seeder.SeedAsync(); // Убедитесь, что SeedAsync корректно обрабатывает существующие данные

                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var initialLogLevelString = config["Serilog:MinimumLevel:Default"];
                if (Enum.TryParse<LogEventLevel>(initialLogLevelString, out var initialLogLevel))
                {
                    _loggingLevelSwitch.MinimumLevel = initialLogLevel;
                    Serilog.Log.Information("Logging level set to {LogLevel} from configuration.", initialLogLevel);
                }
                else
                {
                    _loggingLevelSwitch.MinimumLevel = LogEventLevel.Information; // Уровень по умолчанию
                    Serilog.Log.Warning("Could not parse initial log level from configuration. Defaulting to {LogLevel}.", _loggingLevelSwitch.MinimumLevel);
                }
            }

            var appState = AppHost.Services.GetRequiredService<IApplicationStateService>();

            // Запускаем цикл логина
            bool loggedInSuccessfully = false;
            while (true) // Цикл для повторного показа окна логина при неудаче или выходе
            {
                var loginViewModel = AppHost.Services.GetRequiredService<LoginViewModel>(); // Получаем новый экземпляр ViewModel
                var loginWindow = AppHost.Services.GetRequiredService<LoginWindow>();
                loginWindow.DataContext = loginViewModel; // ИСПРАВЛЕНО: Установка DataContext

                var dialogResult = loginWindow.ShowDialog();

                if (dialogResult == true && appState.CurrentUser != null)
                {
                    loggedInSuccessfully = true;
                    break; // Успешный вход, выходим из цикла
                }
                else
                {
                    // Если пользователь закрыл окно входа или аутентификация не удалась,
                    // и мы не хотим сразу закрывать приложение, можно дать ему выйти или повторить.
                    // В данном случае, если DialogResult не true, приложение закроется после цикла.
                    // Можно добавить кнопку "Выход" на LoginWindow или обработать закрытие окна как выход.
                    if (loginWindow.IsActive) // Если окно еще не закрыто (например, DialogResult был false)
                    {
                        // Пользователь мог нажать "Отмена" или что-то подобное, если такая логика есть в LoginViewModel
                        // Если окно просто закрыто (крестиком), dialogResult будет null.
                        // В этом случае, позволяем приложению завершиться.
                    }
                    break; // Выходим из цикла, если логин не удался или отменен
                }
            }


            if (loggedInSuccessfully)
            {
                var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
                var mainViewModel = AppHost.Services.GetRequiredService<MainViewModel>();

                // Передача сервисов в MainViewModel (если они не инжектируются через конструктор)
                // Предпочтительно инжектировать через конструктор MainViewModel
                mainViewModel.NavigationService = AppHost.Services.GetRequiredService<INavigationService>();
                mainViewModel.PermissionService = AppHost.Services.GetRequiredService<IPermissionService>();
                mainViewModel.ApplicationStateService = appState; // Уже есть ссылка
                mainViewModel.DialogService = AppHost.Services.GetRequiredService<IDialogService>();
                mainViewModel.ServiceProvider = AppHost.Services; // Передаем IServiceProvider

                await mainViewModel.InitializeAsync(); // Инициализация MainViewModel

                Current.MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                Shutdown(); // Завершаем приложение, если вход не выполнен
            }

            base.OnStartup(e);
        }

        private void HandleException(Exception ex, string context)
        {
            Serilog.Log.Fatal(ex, "Unhandled exception in {Context}", context); // Используем Fatal для серьезных ошибок
            try
            {
                var dialogService = AppHost?.Services?.GetService<IDialogService>();
                dialogService?.ShowError("Критическая ошибка",
                                         $"Произошла непредвиденная ошибка в {context}. Приложение может работать нестабильно. Рекомендуется перезапуск.",
                                         ex.ToString());
            }
            catch (Exception dialogEx)
            {
                Serilog.Log.Error(dialogEx, "Failed to show error dialog during global exception handling.");
                MessageBox.Show($"Произошла критическая ошибка: {ex.Message}\nДетали: {ex.ToString()}" +
                                $"\n\nТакже не удалось отобразить диалоговое окно ошибки: {dialogEx.Message}",
                                "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception, "DispatcherUnhandledException");
            e.Handled = true; // Помечаем ошибку как обработанную
        }

        private void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception ?? new Exception("Non-CLR exception from AppDomain"), "AppDomain.CurrentDomain.UnhandledException");
            // Рассмотрите возможность закрытия приложения здесь, если ошибка критическая
            // Environment.Exit(1); // Жесткое закрытие
            // Shutdown(); // Более мягкое WPF закрытие
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            Serilog.Log.Information("Application exiting with code {ExitCode}", e.ApplicationExitCode);
            if (AppHost != null)
            {
                await AppHost.StopAsync();
                AppHost.Dispose();
            }
            Serilog.Log.CloseAndFlush(); // Важно для записи всех логов перед выходом
            base.OnExit(e);
        }
    }
}