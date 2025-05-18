using System;

namespace WpfApp1.Interfaces // Используйте или создайте это пространство имен
{
    public interface ILoggingService
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message, Exception ex = null);
        // При необходимости можно добавить LogDebug, LogTrace, LogFatal
    }
}