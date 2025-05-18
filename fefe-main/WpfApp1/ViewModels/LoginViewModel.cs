// WpfApp1/ViewModels/LoginViewModel.cs
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp1.Commands;
using WpfApp1.Enums;
using WpfApp1.Interfaces;
using WpfApp1.Models;
// Предполагаем, что MainViewModel находится здесь или импортирован
// using WpfApp1.ViewModels; // Если MainViewModel в этом же namespace

namespace WpfApp1.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IApplicationStateService _applicationStateService;
        private readonly ILoggingService _loggingService;
        private readonly INavigationService _navigationService; // <--- ПОЛЕ ДОБАВЛЕНО

        private string _username;
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ObservableCollection<AuthenticationType> AuthTypes { get; }
        private AuthenticationType _selectedAuthType;
        public AuthenticationType SelectedAuthType
        {
            get => _selectedAuthType;
            set => SetProperty(ref _selectedAuthType, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(
            IAuthService authService,
            IApplicationStateService applicationStateService,
            ILoggingService loggingService,
            INavigationService navigationService) // <--- ПАРАМЕТР ДОБАВЛЕН
        {
            _authService = authService;
            _applicationStateService = applicationStateService;
            _loggingService = loggingService;
            _navigationService = navigationService; // <--- ЗАВИСИМОСТЬ СОХРАНЕНА

            AuthTypes = new ObservableCollection<AuthenticationType>((AuthenticationType[])Enum.GetValues(typeof(AuthenticationType)));
            SelectedAuthType = AuthTypes.FirstOrDefault();

            LoginCommand = new RelayCommand<object>(async (param) => await ExecuteLoginCommand(param), CanExecuteLoginCommand);
        }

        private bool CanExecuteLoginCommand(object parameter)
        {
            PasswordBox passwordBox = parameter as PasswordBox;
            return !string.IsNullOrWhiteSpace(Username) && passwordBox != null && passwordBox.SecurePassword.Length > 0;
        }

        private async Task ExecuteLoginCommand(object passwordParameter)
        {
            ErrorMessage = null;
            PasswordBox passwordBox = passwordParameter as PasswordBox;

            if (passwordBox == null || passwordBox.SecurePassword.Length == 0)
            {
                ErrorMessage = "Пароль не может быть пустым.";
                _loggingService.LogWarning("Попытка входа без пароля.");
                return;
            }

            SecureString securePassword = passwordBox.SecurePassword.Copy();

            try
            {
                User authenticatedUser = await _authService.AuthenticateAsync(Username, securePassword, SelectedAuthType);

                if (authenticatedUser != null)
                {
                    _loggingService.LogInformation($"Пользователь '{Username}' ({SelectedAuthType}) успешно вошел в систему. ID={authenticatedUser.UserId}, Роль={authenticatedUser.Role?.RoleName ?? "Не определена"}");
                    _applicationStateService.CurrentUser = authenticatedUser;

                    var loginWindow = System.Windows.Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault();
                    if (loginWindow != null)
                    {
                        loginWindow.DialogResult = true;
                        loginWindow.Close();
                    }

                    // Вызов навигации на главный экран/ViewModel

                }
                else
                {
                    ErrorMessage = _authService.GetLastAuthenticationError() ?? "Ошибка аутентификации. Проверьте данные и попробуйте снова.";
                    _loggingService.LogWarning($"Ошибка аутентификации для пользователя '{Username}' ({SelectedAuthType}): {ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Произошла критическая ошибка: {ex.Message}";
                _loggingService.LogError($"Критическая ошибка при попытке входа пользователя '{Username}': {ex.ToString()}");
            }
            finally
            {
                securePassword?.Dispose();
            }
        }
    }
}