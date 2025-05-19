// WpfApp1/Services/AuthService.cs
// using Microsoft.AspNetCore.Identity; // Этот using можно удалить, если IPasswordHasher<User> больше не используется
using System;
using System.Security;
using System.Threading.Tasks;
using WpfApp1.Enums;
using WpfApp1.Interfaces;
using WpfApp1.Models;
using System.Runtime.InteropServices; // Для Marshal

namespace WpfApp1.Services
{
    public class AuthService : IAuthService
    {
        // ПОЛЕ _passwordHasher УДАЛЕНО
        private readonly IAdAuthService _adAuthService;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly ILoggingService _loggingService; // Ваш сервис-обертка
        private User? _currentUser;
        private string? _lastAuthenticationError;

        public AuthService(
            // ПАРАМЕТР IPasswordHasher<User> passwordHasher УДАЛЕН
            IAdAuthService adAuthService,
            IUserService userService,
            IRoleService roleService,
            ILoggingService loggingService)
        {
            // ПРИСВОЕНИЕ _passwordHasher = passwordHasher УДАЛЕНО
            _adAuthService = adAuthService ?? throw new ArgumentNullException(nameof(adAuthService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        }

        public async Task<User?> AuthenticateAsync(string username, SecureString securePassword, AuthenticationType authType)
        {
            _currentUser = null;
            _lastAuthenticationError = null;

            if (string.IsNullOrWhiteSpace(username))
            {
                _lastAuthenticationError = "Имя пользователя не может быть пустым.";
                _loggingService.LogWarning("Попытка аутентификации с пустым именем пользователя."); // 1 аргумент
                return null;
            }
            if (securePassword == null || securePassword.Length == 0)
            {
                _lastAuthenticationError = "Пароль не может быть пустым.";
                _loggingService.LogWarning($"Попытка аутентификации для пользователя '{username}' без пароля."); // Строковая интерполяция $
                return null;
            }

            if (authType == AuthenticationType.ActiveDirectory)
            {
                var (isAdAuthenticated, adDetails, adErrorMessage) = await _adAuthService.AuthenticateAsync(username, securePassword);
                if (isAdAuthenticated && adDetails != null)
                {
                    var user = await _userService.GetUserByUsernameAsync(username);
                    if (user == null)
                    {
                        _loggingService.LogInformation($"Пользователь AD '{username}' не найден локально. Создание нового локального пользователя.");
                        var defaultRole = await _roleService.GetRoleByNameAsync("User");
                        if (defaultRole == null)
                        {
                            // LogError(string message, Exception ex = null)
                            _loggingService.LogError("Роль по умолчанию 'User' не найдена. Невозможно создать локального пользователя для AD.", null);
                            _lastAuthenticationError = "Ошибка конфигурации: роль по умолчанию для новых AD пользователей не найдена.";
                            return null;
                        }

                        var newUser = new User
                        {
                            Username = adDetails.Username,
                            FullName = adDetails.FullName,
                            Email = adDetails.Email,
                            RoleId = defaultRole.RoleId,
                            Role = defaultRole,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        var generatedPasswordForLocalStore = Guid.NewGuid().ToString("N");
                        var createdUser = await _userService.CreateUserAsync(newUser, generatedPasswordForLocalStore);

                        if (createdUser != null)
                        {
                            _loggingService.LogInformation($"Локальный пользователь '{createdUser.Username}' создан для аутентифицированного через AD пользователя с ролью '{createdUser.Role?.RoleName}'.");
                            _currentUser = createdUser;
                        }
                        else
                        {
                            _loggingService.LogError($"Не удалось создать локального пользователя для AD '{username}'.", null);
                            _lastAuthenticationError = "Не удалось создать локальную учетную запись для пользователя AD.";
                            return null;
                        }
                    }
                    else
                    {
                        if (!user.IsActive)
                        {
                            _loggingService.LogWarning($"Пользователь AD '{username}' найден локально, но неактивен. Отказ в доступе.");
                            _lastAuthenticationError = "Учетная запись пользователя неактивна.";
                            return null;
                        }
                        _loggingService.LogInformation($"Пользователь AD '{username}' найден локально. ID={user.UserId}, RoleId={user.RoleId}.");

                        bool changed = false;
                        if (user.FullName != adDetails.FullName && !string.IsNullOrEmpty(adDetails.FullName))
                        {
                            user.FullName = adDetails.FullName;
                            changed = true;
                        }
                        if (user.Email != adDetails.Email && !string.IsNullOrEmpty(adDetails.Email))
                        {
                            user.Email = adDetails.Email;
                            changed = true;
                        }
                        if (changed)
                        {
                            if (await _userService.UpdateUserAsync(user))
                            {
                                _loggingService.LogInformation($"Данные локального пользователя '{username}' обновлены из AD.");
                            }
                            else
                            {
                                _loggingService.LogWarning($"Не удалось обновить данные локального пользователя '{username}' из AD.");
                            }
                        }
                        _currentUser = user;
                    }
                }
                else
                {
                    _loggingService.LogWarning($"Аутентификация AD не удалась для пользователя '{username}'. Ошибка: {adErrorMessage}");
                    _lastAuthenticationError = adErrorMessage ?? "Ошибка аутентификации Active Directory.";
                }
            }
            else // Локальная аутентификация
            {
                var user = await _userService.GetUserByUsernameAsync(username);
                if (user != null && user.IsActive)
                {
                    IntPtr bstr = IntPtr.Zero;
                    string? plainPassword = null;
                    try
                    {
                        bstr = Marshal.SecureStringToBSTR(securePassword);
                        plainPassword = Marshal.PtrToStringBSTR(bstr);

                        // Используем BCrypt.Net.BCrypt.Verify
                        if (BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash))
                        {
                            _loggingService.LogInformation($"Локальный пользователь '{username}' успешно аутентифицирован (BCrypt).");
                            _currentUser = user;
                        }
                        else
                        {
                            _loggingService.LogWarning($"Локальная аутентификация не удалась для пользователя '{username}'. Неверный пароль (проверка BCrypt).");
                            _lastAuthenticationError = "Неверное имя пользователя или пароль.";
                        }
                    }
                    finally
                    {
                        if (bstr != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr);
                    }
                }
                else if (user != null && !user.IsActive)
                {
                    _loggingService.LogWarning($"Локальная аутентификация не удалась для пользователя '{username}'. Учетная запись неактивна.");
                    _lastAuthenticationError = "Учетная запись пользователя неактивна.";
                }
                else
                {
                    _loggingService.LogWarning($"Локальная аутентификация не удалась для пользователя '{username}'. Пользователь не найден.");
                    _lastAuthenticationError = "Неверное имя пользователя или пароль.";
                }
            }

            if (_currentUser != null && (_currentUser.Role == null && _currentUser.RoleId > 0))
            {
                var role = await _roleService.GetRoleByIdAsync(_currentUser.RoleId);
                if (role != null)
                {
                    _currentUser.Role = role;
                    _loggingService.LogInformation($"Роль '{role.RoleName}' была дополнительно загружена для пользователя '{_currentUser.Username}'.");
                }
                else
                {
                    _loggingService.LogError($"КРИТИЧЕСКАЯ ОШИБКА: Роль для пользователя '{_currentUser.Username}' (RoleId: {_currentUser.RoleId}) не была загружена и не найдена по ID.", null);
                    _lastAuthenticationError = "Критическая ошибка: не удалось загрузить данные о роли пользователя.";
                    _currentUser = null;
                }
            }
            else if (_currentUser != null && _currentUser.Role == null && _currentUser.RoleId == 0)
            {
                _loggingService.LogError($"КРИТИЧЕСКАЯ ОШИБКА: Пользователь '{_currentUser.Username}' не имеет назначенной роли (RoleId is 0).", null);
                _lastAuthenticationError = "Критическая ошибка: пользователю не назначена роль.";
                _currentUser = null;
            }

            return _currentUser;
        }

        public User? GetCurrentUser() => _currentUser;

        public string? GetLastAuthenticationError() => _lastAuthenticationError;

        public async Task LogoutAsync()
        {
            var currentUserName = _currentUser?.Username;
            _currentUser = null;
            _lastAuthenticationError = null;
            _loggingService.LogInformation($"Пользователь '{currentUserName ?? "N/A"}' вышел из системы.");
            await Task.CompletedTask;
        }
    }
}