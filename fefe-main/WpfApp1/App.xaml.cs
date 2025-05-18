// WpfApp1/App.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.Interfaces;
using WpfApp1.Models;
using WpfApp1.Pages;
using WpfApp1.Services;
using WpfApp1.ViewModels;
using WpfApp1.Windows;
using Microsoft.Extensions.Configuration; // Убедитесь, что это есть
using Microsoft.EntityFrameworkCore; // << ДОБАВЛЕНО для UseSqlServer

namespace WpfApp1
{
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }
        private static LoggingLevelSwitch _loggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    // Конфигурация appsettings.json и UserSecrets
                })
                .ConfigureServices((hostContext, services) =>
                {
                    Serilog.Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(hostContext.Configuration) // Читает конфигурацию Serilog из appsettings.json
                        .MinimumLevel.ControlledBy(_loggingLevelSwitch)
                        .Enrich.FromLogContext()
                        .Enrich.WithMachineName()
                        .Enrich.WithThreadId()

                        // .WriteTo.Console() // Можно оставить, если нужно дублирование в консоль
                        // .WriteTo.File($"logs/wpfapp-{DateTime.Now:yyyyMMdd}.txt", // Настройки файла могут быть в appsettings.json
                        //     rollingInterval: RollingInterval.Day,
                        //     retainedFileCountLimit: 7,
                        //     outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
                        // .WriteTo.MSSqlServer( // Настройки MSSqlServer могут быть в appsettings.json
                        //    connectionString: hostContext.Configuration.GetConnectionString("AppDbContext"), // Используем строку из конфигурации
                        //    tableName: "Logs", // Имя таблицы как в конфигурации Serilog
                        //    autoCreateSqlTable: true, // автосоздание таблицы
                        //    columnOptionsSection: hostContext.Configuration.GetSection("Serilog:WriteTo:1:Args:columnOptions")) // путь к columnOptions
                        .CreateLogger();

                    services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
                    services.AddSingleton(_loggingLevelSwitch);
                    services.AddTransient<ISoftwareService, SoftwareService>();
                    services.AddTransient<IDeviceSoftwareService, DeviceSoftwareService>();
                    // Регистрация AppDbContext с использованием строки подключения из конфигурации
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("AppDbContext")),
                        ServiceLifetime.Transient);


                    services.AddSingleton<IApplicationStateService, ApplicationStateService>();
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<INavigationService>(provider => provider.GetRequiredService<MainWindow>());
                    services.AddSingleton<IPermissionService, PermissionService>();

                    services.AddSingleton<IAuthService, AuthService>();
                    services.AddTransient<IUserService, UserService>();
                    services.AddTransient<IRoleService, RoleService>();
                    services.AddTransient<IDeviceService, DeviceService>();
                    services.AddTransient<ILogService, LogService>();
                    services.AddTransient<ILoggingService, LoggingService>(); // Если это ваш сервис для логгирования бизнес-логики
                    services.AddSingleton<IAuthService, AuthService>();

                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<UsersViewModel>();
                    services.AddTransient<AddEditUserViewModel>();
                    services.AddTransient<InventoryViewModel>();
                    services.AddTransient<AddEditDeviceViewModel>();
                    services.AddTransient<LogsViewModel>();
                    services.AddTransient<TicketsViewModel>();

                    services.AddSingleton<MainWindow>();
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<AddEditUserWindow>();
                    services.AddTransient<AddEditDeviceWindow>(); // Раскомментировано, если используется

                    services.AddTransient<UsersPage>();
                    services.AddTransient<InventoryPage>();
                    services.AddTransient<LogsPage>();
                    services.AddTransient<MonitoringPage>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<MonitoringViewModel>();
                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<TicketsPage>();
                    // services.AddTransient<ColumnSettingsPage>(); // Если используется
                    services.AddTransient<ITicketService, TicketService>();
                    services.AddTransient<ITicketCommentService, TicketCommentService>();
                    services.AddTransient<IDataSeeder, DatabaseSeeder>();
                })
                .Build();

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost!.StartAsync();

            using (var scope = AppHost.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
                await seeder.SeedAsync();

                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                // Установка начального уровня логирования из конфигурации
                var initialLogLevelString = config["Serilog:MinimumLevel:Default"]; // Путь к настройке
                if (Enum.TryParse<LogEventLevel>(initialLogLevelString, out var initialLogLevel))
                {
                    _loggingLevelSwitch.MinimumLevel = initialLogLevel;
                }
                else
                {
                    _loggingLevelSwitch.MinimumLevel = LogEventLevel.Information; // Уровень по умолчанию, если не указан или невалиден
                }
            }

            var loginWindow = AppHost.Services.GetRequiredService<LoginWindow>();
            var appState = AppHost.Services.GetRequiredService<IApplicationStateService>();

            loginWindow.ShowDialog();

            if (appState.CurrentUser != null)
            {
                var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
                Current.MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }

            base.OnStartup(e);
        }

        private void HandleException(Exception ex, string context)
        {
            Serilog.Log.Error(ex, $"Global exception in {context}");
            try
            {
                var dialogService = AppHost?.Services?.GetService<IDialogService>();
                // Используем ShowError с тремя аргументами, как предполагается в вашем коде
                dialogService?.ShowError("Критическая ошибка",
                                         $"Произошла непредвиденная ошибка в {context}: {ex.Message}",
                                         ex.ToString()); // ex.ToString() для деталей
            }
            catch (Exception dialogEx)
            {
                Serilog.Log.Error(dialogEx, "Failed to show error dialog.");
                MessageBox.Show($"Произошла критическая ошибка, и не удалось отобразить диалоговое окно ошибки: {ex.Message}\nДетали: {ex.ToString()}",
                                "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception, "DispatcherUnhandledException");
            e.Handled = true; // Помечаем ошибку как обработанную, чтобы приложение не падало
        }

        private void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleException(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved(); // Помечаем исключение как обработанное
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception ?? new Exception("Non-CLR exception from AppDomain"), "AppDomain.CurrentDomain.UnhandledException");
            // Здесь решение о Shutdown() зависит от политики обработки ошибок.
            // Если это критическая ошибка, возможно, стоит завершить приложение.
            // Environment.Exit(1); или Shutdown();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (AppHost != null)
            {
                await AppHost.StopAsync();
                AppHost.Dispose();
            }
            Serilog.Log.CloseAndFlush(); // Закрытие логгера Serilog
            base.OnExit(e);
        }
    }
}