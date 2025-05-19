// WpfApp1/Interfaces/IAuthService.cs
using System.Security;
using System.Threading.Tasks;
using WpfApp1.Enums;
using WpfApp1.Models;

namespace WpfApp1.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Аутентифицирует пользователя.
        /// </summary>
        /// <param name="username">Имя пользователя.</param>
        /// <param name="securePassword">Пароль в виде SecureString.</param>
        /// <param name="authType">Тип аутентификации (локальная или AD).</param>
        /// <returns>Объект User в случае успеха, иначе null. Сообщение об ошибке будет установлено во ViewModel.</returns>
        Task<User> AuthenticateAsync(string username, SecureString securePassword, AuthenticationType authType);

        Task LogoutAsync();
        User GetCurrentUser();
        string GetLastAuthenticationError();
    }
}