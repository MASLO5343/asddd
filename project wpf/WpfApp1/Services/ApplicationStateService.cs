using System; // <--- Добавьте, если еще нету, для EventHandler
using WpfApp1.Interfaces;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class ApplicationStateService : IApplicationStateService
    {
        // 1. Объявляем публичное событие, требуемое интерфейсом
        public event EventHandler? CurrentUserChanged;

        // Приватное поле для хранения значения CurrentUser
        private User? _currentUser; // Используем User? (nullable)

        // 2. Реализуем свойство CurrentUser с логикой вызова события
        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                // Проверяем, изменилось ли значение
                if (_currentUser != value)
                {
                    _currentUser = value; // Устанавливаем новое значение
                    // Вызываем событие, чтобы уведомить подписчиков (MainViewModel)
                    OnCurrentUserChanged();
                }
            }
        }

        // Вспомогательный метод для вызова события (хорошая практика)
        protected virtual void OnCurrentUserChanged()
        {
            CurrentUserChanged?.Invoke(this, EventArgs.Empty);
        }
        public void ClearCurrentUser()
        {
            CurrentUser = null; // Это вызовет событие CurrentUserChanged
        }

        // Конструктор (если нужен для инициализации или других зависимостей)
        // public ApplicationStateService()
        // {
        //     // Начальная инициализация, если требуется
        //     _currentUser = null; 
        // }
    }
}