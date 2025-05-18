// WpfApp1/Interfaces/IPermissionService.cs
using System.Threading.Tasks;
using WpfApp1.Models; // Если используются модели

namespace WpfApp1.Interfaces
{
    public interface IPermissionService
    {
        // Вариант, используемый в UsersViewModel (принимает имя роли)
        bool HasPermission(string permissionName, string roleName);

        // Альтернативный вариант (принимает пользователя) - менее предпочтителен, если сервис не должен знать о User модели напрямую
        // bool HasPermission(User user, string permissionName);

        // Возможно, асинхронный вариант, если проверка прав требует обращения к БД
        // Task<bool> HasPermissionAsync(string permissionName, string roleName);

        // Метод для получения всех разрешений роли (может быть полезен)
        // Task<IEnumerable<string>> GetPermissionsForRoleAsync(string roleName);
    }
}