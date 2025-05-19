using System;         // <--- Добавьте using для EventHandler
using WpfApp1.Models; // Для доступа к User

namespace WpfApp1.Interfaces
{
    public interface IApplicationStateService
    {
        // Свойство для хранения текущего пользователя
        User? CurrentUser { get; set; }
        event EventHandler? CurrentUserChanged;
        void ClearCurrentUser();
    }
}