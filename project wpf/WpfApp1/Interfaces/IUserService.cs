// WpfApp1/Interfaces/IUserService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using WpfApp1.Models;

namespace WpfApp1.Interfaces
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByUsernameAsync(string username);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User> CreateUserAsync(User user, string plainPassword);
        Task<bool> UpdateUserAsync(User userToUpdate);
        Task<bool> DeactivateUserAsync(int userId);
        Task<bool> ActivateUserAsync(int userId);
        Task<bool> ChangePasswordAsync(int userId, string newPassword);

        // ДОБАВЛЕНО: Метод для удаления пользователя
        Task<bool> DeleteUserAsync(int userId);
    }
}