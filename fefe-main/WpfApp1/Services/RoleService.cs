using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WpfApp1.Interfaces;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class RoleService : IRoleService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<RoleService> _logger;

        public RoleService(AppDbContext dbContext, ILogger<RoleService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            _logger.LogInformation("Запрос роли по ID: {RoleId}", roleId);
            try
            {
                return await _dbContext.Roles.FindAsync(roleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении роли по ID: {RoleId}", roleId);
                return null;
            }
        }

        public async Task<Role?> GetRoleByNameAsync(string roleName)
        {
            _logger.LogInformation("Запрос роли по имени: {RoleName}", roleName);
            if (string.IsNullOrWhiteSpace(roleName)) // Добавлена проверка на пустую строку или null
            {
                _logger.LogWarning("GetRoleByNameAsync вызван с пустым или null именем роли.");
                return null;
            }
            try
            {
                // ИЗМЕНЕНИЕ: Заменено .Equals(roleName, StringComparison.OrdinalIgnoreCase)
                // на преобразование к нижнему регистру для обеих строк.
                // Это позволяет EF Core транслировать запрос в SQL.
                string lowerRoleName = roleName.ToLower();
                return await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == lowerRoleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении роли по имени: {RoleName}", roleName);
                return null;
            }
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            _logger.LogInformation("Запрос всех ролей.");
            try
            {
                return await _dbContext.Roles.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении всех ролей.");
                return Enumerable.Empty<Role>();
            }
        }

        public async Task<Role> CreateRoleAsync(Role role)
        {
            _logger.LogInformation("Попытка создания новой роли: {RoleName}", role.RoleName);
            if (role == null)
            {
                _logger.LogError("CreateRoleAsync вызван с null объектом роли.");
                throw new ArgumentNullException(nameof(role));
            }
            if (string.IsNullOrWhiteSpace(role.RoleName)) // Добавлена проверка на пустое имя роли
            {
                _logger.LogError("Попытка создать роль с пустым или null именем.");
                throw new ArgumentException("Имя роли не может быть пустым.", nameof(role.RoleName));
            }

            try
            {
                // ИЗМЕНЕНИЕ: Заменено .Equals(role.RoleName, StringComparison.OrdinalIgnoreCase)
                // на преобразование к нижнему регистру для обеих строк.
                string lowerRoleName = role.RoleName.ToLower();
                bool nameExists = await _dbContext.Roles.AnyAsync(r => r.RoleName.ToLower() == lowerRoleName);
                if (nameExists)
                {
                    _logger.LogWarning("Попытка создать роль с уже существующим именем: {RoleName}", role.RoleName);
                    throw new InvalidOperationException($"Роль с именем '{role.RoleName}' уже существует.");
                }

                _dbContext.Roles.Add(role);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Роль '{RoleName}' (ID: {RoleId}) успешно создана.", role.RoleName, role.RoleId);
                return role;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка DbUpdateException при создании роли: {RoleName}", role.RoleName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Общая ошибка при создании роли: {RoleName}", role.RoleName);
                throw;
            }
        }

        public async Task<bool> UpdateRoleAsync(Role role)
        {
            _logger.LogInformation("Попытка обновления роли ID: {RoleId}", role.RoleId);
            if (role == null)
            {
                _logger.LogError("UpdateRoleAsync вызван с null объектом роли.");
                throw new ArgumentNullException(nameof(role));
            }
            if (string.IsNullOrWhiteSpace(role.RoleName)) // Добавлена проверка на пустое имя роли
            {
                _logger.LogError("Попытка обновить роль ID {RoleId} на пустое или null имя.", role.RoleId);
                throw new ArgumentException("Имя роли не может быть пустым.", nameof(role.RoleName));
            }

            try
            {
                // ИЗМЕНЕНИЕ: Заменено .Equals(role.RoleName, StringComparison.OrdinalIgnoreCase)
                // на преобразование к нижнему регистру для обеих строк.
                string lowerRoleName = role.RoleName.ToLower();
                bool nameExistsForOtherRole = await _dbContext.Roles.AnyAsync(r => r.RoleName.ToLower() == lowerRoleName && r.RoleId != role.RoleId);
                if (nameExistsForOtherRole)
                {
                    _logger.LogWarning("Попытка обновить роль ID {RoleId} на имя '{RoleName}', которое уже используется другой ролью.", role.RoleId, role.RoleName);
                    throw new InvalidOperationException($"Имя роли '{role.RoleName}' уже используется другой ролью.");
                }

                // Добавлена проверка, существует ли роль, которую пытаемся обновить
                var existingRole = await _dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == role.RoleId);
                if (existingRole == null)
                {
                    _logger.LogWarning("Попытка обновить несуществующую роль. ID: {RoleId}", role.RoleId);
                    return false; // Или выбросить исключение, в зависимости от логики приложения
                }

                _dbContext.Roles.Update(role); // EF Core начнет отслеживать изменения
                int affectedRows = await _dbContext.SaveChangesAsync();
                bool success = affectedRows > 0;
                if (success)
                {
                    _logger.LogInformation("Роль ID {RoleId} успешно обновлена.", role.RoleId);
                }
                else
                {
                    _logger.LogWarning("Обновление роли ID {RoleId} не затронуло ни одной строки. Возможно, данные не изменились или роль была удалена параллельно.", role.RoleId);
                }
                return success;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Ошибка DbUpdateConcurrencyException при обновлении роли ID {RoleId}. Возможно, роль была изменена или удалена.", role.RoleId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении роли ID {RoleId}", role.RoleId);
                return false;
            }
        }

        public async Task<bool> DeleteRoleAsync(int roleId)
        {
            _logger.LogInformation("Попытка удаления роли ID: {RoleId}", roleId);
            var roleToDelete = await _dbContext.Roles.FindAsync(roleId);
            if (roleToDelete == null)
            {
                _logger.LogWarning("Роль ID {RoleId} не найдена для удаления.", roleId);
                return false;
            }

            bool isRoleInUse = await _dbContext.Users.AnyAsync(u => u.RoleId == roleId);
            if (isRoleInUse)
            {
                _logger.LogWarning("Попытка удалить роль ID {RoleId}, которая используется пользователями.", roleId);
                throw new InvalidOperationException("Нельзя удалить роль, так как она назначена одному или нескольким пользователям.");
            }

            try
            {
                _dbContext.Roles.Remove(roleToDelete);
                int affectedRows = await _dbContext.SaveChangesAsync();
                bool success = affectedRows > 0;
                if (success)
                {
                    _logger.LogInformation("Роль ID {RoleId} успешно удалена.", roleId);
                }
                else
                {
                    _logger.LogWarning("Удаление роли ID {RoleId} не затронуло ни одной строки.", roleId);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении роли ID {RoleId}", roleId);
                return false;
            }
        }

        public async Task<bool> RoleExistsAsync(int roleId)
        {
            _logger.LogInformation("Проверка существования роли с ID: {RoleId}", roleId);
            if (roleId <= 0)
            {
                _logger.LogWarning("RoleExistsAsync вызван с некорректным ID роли: {RoleId}", roleId);
                return false;
            }
            try
            {
                bool exists = await _dbContext.Roles.AnyAsync(r => r.RoleId == roleId);
                if (exists)
                {
                    _logger.LogDebug("Роль с ID {RoleId} существует.", roleId);
                }
                else
                {
                    _logger.LogDebug("Роль с ID {RoleId} не найдена.", roleId);
                }
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке существования роли с ID {RoleId}", roleId);
                return false;
            }
        }
    }
}