// fefe-main/WpfApp1/Services/LoggingService.cs
using Microsoft.Extensions.Logging; // Используем стандартный ILogger
using System;
using WpfApp1.Interfaces; // Подключаем пространство имен с интерфейсом

namespace WpfApp1.Services // Ваше пространство имен для сервисов
{
    public class LoggingService : ILoggingService // Реализуем интерфейс
    {
        // Запрашиваем ILogger<LoggingService> через конструктор.
        // DI предоставит логгер, настроенный на использование Serilog.
        private readonly ILogger<LoggingService> _logger;

        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        public void LogError(string message, Exception ex = null)
        {
            // Стандартный ILogger использует такой формат для ошибок с исключениями
            _logger.LogError(ex, message);
        }
    }
}