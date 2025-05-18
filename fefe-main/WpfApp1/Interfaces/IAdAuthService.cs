// WpfApp1/Interfaces/IAdAuthService.cs
using System.Security;
using System.Threading.Tasks;

namespace WpfApp1.Interfaces
{
    public class AdUserDetailsDto
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }

    public interface IAdAuthService
    {
        /// <summary>
        /// Аутентифицирует пользователя в Active Directory используя SecureString.
        /// </summary>
        /// <param name="username">Имя пользователя.</param>
        /// <param name="securePassword">Пароль в виде SecureString.</param>
        /// <returns>
        /// Кортеж: (
        ///   bool IsSuccess, 
        ///   AdUserDetailsDto UserDetails, 
        ///   string ErrorMessage - содержит сообщение об ошибке в случае неудачи, иначе null
        /// ).
        /// </returns>
        Task<(bool IsSuccess, AdUserDetailsDto UserDetails, string ErrorMessage)> AuthenticateAsync(string username, SecureString securePassword);
    }
}