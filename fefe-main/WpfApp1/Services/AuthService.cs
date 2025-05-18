// WpfApp1/Services/AuthService.cs
using Microsoft.AspNetCore.Identity; // Для IPasswordHasher<User>
using System;
using System.Security;
using System.Threading.Tasks;
using WpfApp1.Enums;
using WpfApp1.Interfaces;
using WpfApp1.Models; // Для User

namespace WpfApp1.Services
{
    public class AuthService : IAuthService
    {
        private readonly IPasswordHasher<User> _passwordHasher; // Типизирован User
        private readonly IAdAuthService _adAuthService;
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly ILoggingService _loggingService; // Используем ILoggingService
        private User? _currentUser; // User может быть null
        private string? _lastAuthenticationError; // Может быть null

        public AuthService(
            IPasswordHasher<User> passwordHasher, // Типизирован User
            IAdAuthService adAuthService,
            IUserService userService,
            IRoleService roleService,
            ILoggingService loggingService) // Используем ILoggingService
        {
            _passwordHasher = passwordHasher;
            _adAuthService = adAuthService;
            _userService = userService;
            _roleService = roleService;
            _loggingService = loggingService;
        }

        public async Task<User?> AuthenticateAsync(string username, SecureString securePassword, AuthenticationType authType)
        {
            _currentUser = null;
            _lastAuthenticationError = null;

            if (authType == AuthenticationType.ActiveDirectory)
            {
                var (isAdAuthenticated, adDetails, adErrorMessage) = await _adAuthService.AuthenticateAsync(username, securePassword);
                if (isAdAuthenticated && adDetails != null) // Проверка adDetails на null
                {
                    var user = await _userService.GetUserByUsernameAsync(username);
                    if (user == null)
                    {
                        _loggingService.LogInformation($"Пользователь AD '{username}' не найден локально. Создание нового локального пользователя.");
                        var defaultRole = await _roleService.GetRoleByNameAsync("User");
                        if (defaultRole == null)
                        {
                            _loggingService.LogError("Роль по умолчанию 'User' не найдена. Невозможно создать локального пользователя для AD.");
                            _lastAuthenticationError = "Ошибка конфигурации: роль по умолчанию для новых AD пользователей не найдена.";
                            return null;
                        }

                        var newUser = new User
                        {
                            Username = adDetails.Username,
                            // PasswordHash = "", // Будет перезаписан CreateUserAsync
                            FullName = adDetails.FullName,
                            Email = adDetails.Email,
                            RoleId = defaultRole.RoleId,
                            Role = defaultRole,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        // Генерируем случайный пароль для CreateUserAsync
                        var generatedPassword = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                        var createdUser = await _userService.CreateUserAsync(newUser, generatedPassword);

                        if (createdUser != null)
                        {
                            _loggingService.LogInformation($"Локальный пользователь '{createdUser.Username}' создан для аутентифицированного через AD пользователя с ролью '{createdUser.Role?.RoleName}'.");
                            _currentUser = createdUser;
                        }
                        else
                        {
                            _loggingService.LogError($"Не удалось создать локального пользователя для AD '{username}'.");
                            _lastAuthenticationError = "Не удалось создать локальную учетную запись для пользователя AD.";
                            return null;
                        }
                    }
                    else
                    {
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
                            await _userService.UpdateUserAsync(user); // Предполагается, что UpdateUserAsync корректно обрабатывает обновление
                            _loggingService.LogInformation($"Данные локального пользователя '{username}' обновлены из AD.");
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
                    string? plainPassword = null; // string? чтобы можно было присвоить null
                    try
                    {
                        bstr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(securePassword);
                        plainPassword = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(bstr);

                        // Проверка пароля с использованием IPasswordHasher<User>
                        var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, plainPassword ?? string.Empty);
                        if (passwordVerificationResult == PasswordVerificationResult.Success || passwordVerificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                        {
                            _loggingService.LogInformation($"Локальный пользователь '{username}' успешно аутентифицирован.");
                            _currentUser = user;
                        }
                        else
                        {
                            _loggingService.LogWarning($"Локальная аутентификация не удалась для пользователя '{username}'. Неверный пароль.");
                            _lastAuthenticationError = "Неверное имя пользователя или пароль.";
                        }
                    }
                    finally
                    {
                        if (bstr != IntPtr.Zero) System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(bstr);
                        if (plainPassword != null) Array.Clear(plainPassword.ToCharArray(), 0, plainPassword.Length); // Очистка пароля из памяти
                        plainPassword = null;
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

            if (_currentUser != null && _currentUser.Role == null && _currentUser.RoleId > 0)
            {
                _loggingService.LogError($"КРИТИЧЕСКАЯ ОШИБКА: Роль для пользователя '{_currentUser.Username}' (RoleId: {_currentUser.RoleId}) не была загружена сервисом IUserService.");
                _lastAuthenticationError = "Критическая ошибка: не удалось загрузить данные о роли пользователя.";
                // _currentUser = null; // Раскомментируйте, если сессия должна быть прервана
            }

            return _currentUser;
        }

        public User? GetCurrentUser() => _currentUser;

        public string? GetLastAuthenticationError() => _lastAuthenticationError;

        public async Task LogoutAsync()
        {
            var currentUserName = _currentUser?.Username; // Сохраним имя для лога перед сбросом
            _currentUser = null;
            _lastAuthenticationError = null;
            _loggingService.LogInformation($"Пользователь '{currentUserName ?? "N/A"}' вышел из системы.");
            await Task.CompletedTask;
        }
    }
}