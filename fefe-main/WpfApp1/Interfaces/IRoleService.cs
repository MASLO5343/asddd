using System.Collections.Generic;
using System.Threading.Tasks;
using WpfApp1.Models; // Для Role

namespace WpfApp1.Interfaces
{
    public interface IRoleService
    {
        Task<Role?> GetRoleByIdAsync(int roleId);
        Task<Role?> GetRoleByNameAsync(string roleName);
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task<Role> CreateRoleAsync(Role role);
        Task<bool> UpdateRoleAsync(Role role); // Предполагаю, что такой метод может существовать или понадобится
        Task<bool> DeleteRoleAsync(int roleId); // Предполагаю, что такой метод может существовать или понадобится

        /// <summary>
        /// Проверяет, существует ли роль с указанным ID.
        /// </summary>
        /// <param name="roleId">ID роли для проверки.</param>
        /// <returns>True, если роль существует, иначе false.</returns>
        Task<bool> RoleExistsAsync(int roleId);
    }
}