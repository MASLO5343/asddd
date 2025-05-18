// WpfApp1/Services/UserService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WpfApp1.Interfaces;
using WpfApp1.Models;
using BCrypt.Net; // Для хеширования пароля
using Serilog.Context; // Для LogContext

namespace WpfApp1.Services
{
    public class UserService : IUserService // Убедитесь, что эта строка соответствует имени вашего интерфейса, если оно другое
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IApplicationStateService _applicationStateService;
        // private readonly IIpAddressProvider _ipAddressProvider; // ПРИМЕР: Внедрите зависимость, если у вас есть способ получить IP

        public UserService(AppDbContext context, ILogger<UserService> logger, IApplicationStateService applicationStateService/*, IIpAddressProvider ipAddressProvider = null*/)
        {
            _context = context;
            _logger = logger;
            _applicationStateService = applicationStateService;
            // _ipAddressProvider = ipAddressProvider; // ПРИМЕР
        }

        // Вспомогательный метод для установки общих свойств контекста логирования
        private void SetupLogContext(string actionName, string? targetEntityInfo = null)
        {
            var currentUser = _applicationStateService.CurrentUser;
            LogContext.PushProperty("UserId", currentUser?.UserId);
            LogContext.PushProperty("UserName", currentUser?.Username);
            LogContext.PushProperty("Action", actionName);
            LogContext.PushProperty("TableAffected", "Users");
            // LogContext.PushProperty("IPAddress", _ipAddressProvider?.GetLocalIpAddress()); // ПРИМЕР: Добавьте IP-адрес, если доступно
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            SetupLogContext("GetUserByUsername", $"Username: {username}");
            _logger.LogDebug("Запрос пользователя по имени: {Username}", username);
            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
            if (user == null)
            {
                _logger.LogWarning("Пользователь {Username} не найден.", username);
            }
            else if (!user.IsActive)
            {
                _logger.LogWarning("Пользователь {Username} найден, но не активен.", username);
            }
            else
            {
                _logger.LogInformation("Пользователь {Username} успешно найден.", username);
            }
            return user;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            SetupLogContext("GetUserById", $"UserId: {userId}");
            _logger.LogDebug("Запрос пользователя по ID: {UserId}", userId);
            var user = await _context.Users
                                     .Include(u => u.Role)
                                     .FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                _logger.LogWarning("Пользователь с ID {UserId} не найден.", userId);
            }
            else
            {
                _logger.LogInformation("Пользователь с ID {UserId} успешно найден.", userId);
            }
            return user;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            SetupLogContext("GetAllUsers");
            _logger.LogDebug("Запрос всех пользователей.");
            var users = await _context.Users.Include(u => u.Role).ToListAsync();
            _logger.LogInformation("Успешно получено {UserCount} пользователей.", users.Count);
            return users;
        }

        public async Task<User> CreateUserAsync(User user, string password)
        {
            SetupLogContext("CreateUser", $"Username: {user.Username}");

            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == user.Username.ToLower()))
            {
                _logger.LogWarning("Попытка создать пользователя с уже существующим именем: {Username}", user.Username);
                throw new ArgumentException("Пользователь с таким именем уже существует.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var currentUser = _applicationStateService.CurrentUser;
            _logger.LogInformation("Пользователь {Username} (ID: {NewUserId}) успешно создан пользователем {CurrentUsername}.",
                                   user.Username, user.UserId, currentUser?.Username ?? "System");
            return user;
        }

        // Исправленная реализация для CS0535
        public async Task<bool> UpdateUserAsync(User userToUpdate)
        {
            SetupLogContext("UpdateUser", $"UserId: {userToUpdate.UserId}");

            var existingUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userToUpdate.UserId);
            if (existingUser == null)
            {
                _logger.LogError("Попытка обновить несуществующего пользователя с ID: {UserId}", userToUpdate.UserId);
                // throw new ArgumentException("Пользователь не найден.");
                return false; // Или выбросить исключение в соответствии с вашей стратегией обработки ошибок
            }

            // Проверка, изменяется ли имя пользователя на уже существующее у другого пользователя
            if (existingUser.Username.ToLower() != userToUpdate.Username.ToLower() &&
                await _context.Users.AnyAsync(u => u.Username.ToLower() == userToUpdate.Username.ToLower() && u.UserId != userToUpdate.UserId))
            {
                _logger.LogWarning("Попытка изменить имя пользователя на уже существующее: {Username}", userToUpdate.Username);
                // throw new ArgumentException("Пользователь с таким именем уже существует.");
                return false; // Или выбросить исключение
            }

            existingUser.Username = userToUpdate.Username;
            existingUser.FullName = userToUpdate.FullName;
            existingUser.Email = userToUpdate.Email;
            existingUser.RoleId = userToUpdate.RoleId;
            // Примечание: IsActive обычно управляется методами Activate/Deactivate, но если передано, оно обновляется.
            // Изменение пароля должно обрабатываться ChangePasswordAsync.

            try
            {
                await _context.SaveChangesAsync();
                var currentUser = _applicationStateService.CurrentUser;
                _logger.LogInformation("Пользователь {Username} (ID: {UserId}) успешно обновлен пользователем {CurrentUsername}.",
                                       existingUser.Username, existingUser.UserId, currentUser?.Username ?? "System");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении пользователя {Username} (ID: {UserId}).", existingUser.Username, existingUser.UserId);
                return false;
            }
        }

        // Исправленная реализация для CS0535
        public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
        {
            SetupLogContext("ChangePassword", $"UserId: {userId}");
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Пользователь с ID {UserId} не найден для смены пароля.", userId);
                return false;
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                _logger.LogWarning("Попытка установить пустой пароль для пользователя ID: {UserId}", userId);
                return false; // Или выбросить ArgumentException
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Пароль для пользователя {Username} (ID: {UserId}) успешно изменен.", user.Username, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при смене пароля для пользователя {Username} (ID: {UserId}).", user.Username, userId);
                return false;
            }
        }

        // Исправленная реализация для CS0535 
        // На основе "Правок", IsActive используется для "Активен/удален".
        // Я предоставляю и Activate, и Deactivate в соответствии с типичным дизайном интерфейса.
        public async Task<bool> ActivateUserAsync(int userId)
        {
            SetupLogContext("ActivateUser", $"UserId: {userId}");
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Попытка активировать несуществующего пользователя с ID: {UserId}", userId);
                return false;
            }
            if (user.IsActive)
            {
                _logger.LogInformation("Пользователь {Username} (ID: {UserId}) уже активен.", user.Username, userId);
                return true; // Уже активен
            }

            user.IsActive = true;
            try
            {
                await _context.SaveChangesAsync();
                var currentUser = _applicationStateService.CurrentUser;
                _logger.LogInformation("Пользователь {Username} (ID: {UserId}) успешно активирован пользователем {CurrentUsername}.",
                                       user.Username, user.UserId, currentUser?.Username ?? "System");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при активации пользователя {Username} (ID: {UserId}).", user.Username, user.UserId);
                return false;
            }
        }

        // Исправленная реализация для CS0535
        public async Task<bool> DeactivateUserAsync(int userId)
        {
            SetupLogContext("DeactivateUser", $"UserId: {userId}");
            var user = await _context.Users.FindAsync(userId);
            var performingUser = _applicationStateService.CurrentUser;

            if (user == null)
            {
                _logger.LogWarning("Попытка деактивировать несуществующего пользователя с ID: {UserId}", userId);
                return false;
            }

            if (user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                (performingUser != null && user.UserId == performingUser.UserId))
            {
                _logger.LogWarning("Попытка деактивировать системного администратора или текущего пользователя ({Username}) была заблокирована.", user.Username);
                // throw new InvalidOperationException("Нельзя деактивировать администратора или самого себя.");
                return false;
            }

            if (!user.IsActive)
            {
                _logger.LogInformation("Пользователь {Username} (ID: {UserId}) уже неактивен.", user.Username, userId);
                return true; // Уже неактивен
            }

            user.IsActive = false;
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Пользователь {Username} (ID: {UserId}) был деактивирован пользователем {CurrentUsername}.",
                                       user.Username, user.UserId, performingUser?.Username ?? "System");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при деактивации пользователя {Username} (ID: {UserId}).", user.Username, user.UserId);
                return false;
            }
        }

        // Исправленная реализация для CS0738 и имени метода из вашего IUserService.cs
        // DeleteUserAsync будет тем, который возвращает Task<bool>
        public async Task<bool> DeleteUserAsync(int userId) // Мягкое удаление
        {
            // Этот метод теперь корректно возвращает Task<bool>
            // Он реализует "мягкое удаление", устанавливая IsActive в false.
            SetupLogContext("DeleteUser (Soft)", $"UserId: {userId}");
            var user = await _context.Users.FindAsync(userId);
            var performingUser = _applicationStateService.CurrentUser;

            if (user == null)
            {
                _logger.LogWarning("Попытка удалить (пометить неактивным) несуществующего пользователя с ID: {UserId}", userId);
                return false; // Пользователь не найден
            }

            // Предотвращение удаления 'admin' или самодеактивации
            if (user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                (performingUser != null && user.UserId == performingUser.UserId))
            {
                _logger.LogWarning("Попытка удалить (пометить неактивным) системного администратора или текущего пользователя ({Username}) была заблокирована.", user.Username);
                // Рассмотрите возможность выброса InvalidOperationException, если это жесткое бизнес-правило
                // throw new InvalidOperationException("Cannot deactivate admin or self.");
                return false; // Операция не разрешена
            }

            if (!user.IsActive) // Если уже "удален" (неактивен)
            {
                _logger.LogInformation("Пользователь {Username} (ID: {UserId}) уже помечен как неактивный.", user.Username, userId);
                return true; // Изменений не требуется, считается успехом
            }

            user.IsActive = false; // Мягкое удаление
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Пользователь {Username} (ID: {UserId}) был помечен как неактивный (удален) пользователем {CurrentUsername}.",
                                       user.Username, user.UserId, performingUser?.Username ?? "System");
                return true; // Успех
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при пометке пользователя {Username} (ID: {UserId}) как неактивного (удаление).", user.Username, user.UserId);
                return false; // Ошибка во время сохранения
            }
        }
    }
}