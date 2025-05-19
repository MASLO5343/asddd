// WpfApp1/Services/AdAuthService.cs
using System;
using System.DirectoryServices.AccountManagement;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using WpfApp1.Interfaces;
using WpfApp1.Services; // Если ILoggingService здесь же, иначе уточнить using
using System.Runtime.InteropServices; // Добавлено для Marshal

// Предполагается, что AdUserDetailsDto определен где-то так:
// public class AdUserDetailsDto
// {
//     public string Username { get; set; }
//     public string FullName { get; set; }
//     public string Email { get; set; }
// }


namespace WpfApp1.Services
{
    public class AdAuthService : IAdAuthService
    {
        private readonly string _domainName;
        private readonly ILoggingService _loggingService;

        public AdAuthService(IConfiguration configuration, ILoggingService loggingService)
        {
            _domainName = configuration["ActiveDirectory:DomainName"];
            _loggingService = loggingService;

            if (string.IsNullOrEmpty(_domainName))
            {
                _loggingService.LogError("Домен Active Directory не сконфигурирован в appsettings.json (ActiveDirectory:DomainName). AD аутентификация будет недоступна.");
            }
        }

        public async Task<(bool IsSuccess, AdUserDetailsDto UserDetails, string ErrorMessage)> AuthenticateAsync(string username, SecureString securePassword)
        {
            if (string.IsNullOrEmpty(_domainName))
            {
                _loggingService.LogWarning("Аутентификация AD невозможна: домен не сконфигурирован.");
                return (false, null, "Сервис AD не настроен: домен не указан.");
            }
            if (string.IsNullOrEmpty(username) || securePassword == null || securePassword.Length == 0)
            {
                _loggingService.LogWarning("Аутентификация AD: имя пользователя или пароль не указаны.");
                return (false, null, "Имя пользователя и пароль должны быть указаны.");
            }

            // Явно указываем тип результата для Task.Run
            return await Task.Run<(bool IsSuccess, AdUserDetailsDto UserDetails, string ErrorMessage)>(() =>
            {
                IntPtr bstr = IntPtr.Zero;
                string plainPassword = null;

                try
                {
                    bstr = Marshal.SecureStringToBSTR(securePassword);
                    plainPassword = Marshal.PtrToStringBSTR(bstr);

                    try
                    {
                        using (var context = new PrincipalContext(ContextType.Domain, _domainName))
                        {
                            bool isValid = context.ValidateCredentials(username, plainPassword, ContextOptions.Negotiate);
                            if (isValid)
                            {
                                UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
                                AdUserDetailsDto details = null;
                                if (userPrincipal != null)
                                {
                                    details = new AdUserDetailsDto
                                    {
                                        Username = userPrincipal.SamAccountName,
                                        FullName = userPrincipal.DisplayName,
                                        Email = userPrincipal.EmailAddress
                                    };
                                    _loggingService.LogInformation($"Пользователь '{username}' успешно аутентифицирован через AD.");
                                }
                                else
                                {
                                    _loggingService.LogWarning($"Пользователь '{username}' аутентифицирован через AD, но не удалось получить детали UserPrincipal. Используется только имя пользователя.");
                                    details = new AdUserDetailsDto { Username = username };
                                }
                                return (true, details, null);
                            }
                            else
                            {
                                _loggingService.LogWarning($"AD аутентификация не удалась для пользователя '{username}'. Неверные учетные данные.");
                                return (false, null, "Неверное имя пользователя или пароль Active Directory.");
                            }
                        }
                    }
                    catch (PrincipalServerDownException ex)
                    {
                        _loggingService.LogError($"Сервер AD недоступен (домен: '{_domainName}'). Ошибка: {ex.Message}");
                        return (false, null, $"Сервер Active Directory ({_domainName}) недоступен.");
                    }
                    catch (Exception ex) // Более общий Exception для внутренних ошибок AD
                    {
                        _loggingService.LogError($"Ошибка аутентификации AD для пользователя '{username}'. Домен: '{_domainName}'. Ошибка: {ex.ToString()}");
                        return (false, null, "Произошла ошибка при попытке аутентификации через Active Directory.");
                    }
                }
                catch (Exception ex) // Исключения, связанные с Task.Run или преобразованием пароля
                {
                    _loggingService.LogError($"Критическая ошибка при попытке вызова аутентификации AD для пользователя '{username}'. Ошибка: {ex.ToString()}");
                    return (false, null, "Внутренняя ошибка сервера при аутентификации AD.");
                }
                finally
                {
                    if (bstr != IntPtr.Zero)
                    {
                        Marshal.ZeroFreeBSTR(bstr);
                    }
                    plainPassword = null;
                }
            });
        }
    }
}