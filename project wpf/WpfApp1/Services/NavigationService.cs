using System;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using WpfApp1.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq; // Для Assembly.GetTypes()

namespace WpfApp1.Services
{
    public class NavigationService : INavigationService
    {
        private Frame _mainFrame;
        private IServiceProvider _serviceProvider;
        private readonly ILogger<NavigationService> _logger;

        public NavigationService(ILogger<NavigationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Initialize(Frame frame, IServiceProvider serviceProvider)
        {
            _mainFrame = frame ?? throw new ArgumentNullException(nameof(frame));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger.LogInformation("NavigationService initialized with Frame: {FrameName}", frame.Name);
        }

        public void NavigateTo(Type pageViewModelType)
        {
            if (_mainFrame == null || _serviceProvider == null)
            {
                _logger.LogError("NavigationService не инициализирован. Вызовите Initialize перед навигацией.");
                throw new InvalidOperationException("NavigationService is not initialized. Call Initialize first.");
            }

            try
            {
                string viewModelName = pageViewModelType.Name;
                string pageName = viewModelName.EndsWith("ViewModel") ? viewModelName.Substring(0, viewModelName.Length - "ViewModel".Length) : viewModelName;

                // Ищем страницу в WpfApp1.Pages или WpfApp1.Views (старый вариант)
                string pageTypeNameInPages = $"WpfApp1.Pages.{pageName}Page";
                string pageTypeNameInViews = $"WpfApp1.Views.{pageName}Page"; // Для обратной совместимости или если страницы в Views

                Type? actualPageType = Type.GetType(pageTypeNameInPages) ?? Type.GetType(pageTypeNameInViews);

                // Если не нашли по полному имени, пробуем найти в сборке (более гибкий вариант)
                if (actualPageType == null)
                {
                    var assembly = typeof(App).Assembly;
                    actualPageType = assembly.GetTypes().FirstOrDefault(t =>
                        (t.Namespace == "WpfApp1.Pages" || t.Namespace == "WpfApp1.Views") && // Ищем в обоих пространствах
                        t.Name == pageName + "Page");
                }

                if (actualPageType == null)
                {
                    _logger.LogError("Тип страницы не найден для ViewModel: {ViewModelType}. Ожидаемое имя страницы: {ExpectedPageNameInPages} или {ExpectedPageNameInViews}", pageViewModelType.FullName, pageTypeNameInPages, pageTypeNameInViews);
                    throw new ArgumentException($"Page not found for ViewModel {pageViewModelType.Name}. Expected: {pageName}Page in WpfApp1.Pages or WpfApp1.Views");
                }

                var pageInstance = _serviceProvider.GetRequiredService(actualPageType);
                if (pageInstance is Page || pageInstance is UserControl)
                {
                    _mainFrame.Navigate(pageInstance);
                    _logger.LogInformation("Навигация на страницу {PageType} для ViewModel {ViewModelType} успешна.", actualPageType.FullName, pageViewModelType.FullName);
                }
                else
                {
                    _logger.LogError("Запрошенный сервис {PageType} не является страницей (Page или UserControl).", actualPageType.FullName);
                    throw new ArgumentException($"The resolved service for {actualPageType.Name} is not a Page or UserControl.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка навигации на страницу для ViewModel: {ViewModelType}", pageViewModelType.FullName);
                throw;
            }
        }
    }
}