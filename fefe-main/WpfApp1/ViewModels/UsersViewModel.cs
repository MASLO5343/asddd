// fefe-main/WpfApp1/ViewModels/UsersViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.Interfaces; // Ensure this includes IApplicationStateService
using WpfApp1.Messages;
using WpfApp1.Models;
using WpfApp1.Constants;

namespace WpfApp1.ViewModels
{
    public partial class UsersViewModel : BaseViewModel, IRecipient<UserAddedMessage>, IRecipient<UserUpdatedMessage>
    {
        private readonly IUserService _userService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<UsersViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessenger _messenger;
        private readonly IPermissionService _permissionService;
        private readonly IRoleService _roleService;
        private readonly IApplicationStateService _applicationStateService; // ADDED

        [ObservableProperty]
        private ObservableCollection<User> _users = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditUserCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteUserCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleUserStatusCommand))]
        private User? _selectedUser;

        public UsersViewModel(
            IUserService userService,
            IDialogService dialogService,
            ILogger<UsersViewModel> logger,
            IServiceProvider serviceProvider,
            IMessenger messenger,
            IPermissionService permissionService,
            IRoleService roleService,
            IApplicationStateService applicationStateService) // ADDED applicationStateService
        {
            _userService = userService;
            _dialogService = dialogService;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _messenger = messenger;
            _permissionService = permissionService;
            _roleService = roleService;
            _applicationStateService = applicationStateService; // ADDED assignment

            Title = "User Management";
            _messenger.RegisterAll(this);
            _ = LoadUsersAsync();
        }

        public void Cleanup()
        {
            _messenger.UnregisterAll(this);
            _logger.LogInformation("UsersViewModel cleaned up and unregistered from messenger.");
        }

        [RelayCommand]
        private async Task LoadUsersAsync()
        {
            IsBusy = true;
            _logger.LogInformation("Loading users...");
            try
            {
                var userList = await _userService.GetAllUsersAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Clear();
                    foreach (var user in userList)
                    {
                        Users.Add(user);
                    }
                    _logger.LogInformation("Successfully loaded {Count} users.", Users.Count);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users.");
                _dialogService.ShowError("Ошибка", "Не удалось загрузить пользователей.", ex.ToString());
            }
            finally
            {
                IsBusy = false;
            }
        }

        // CORRECTED: CanExecute method for AddUserCommand with roleName
        private bool CanAddUser()
        {
            var currentUserRoleName = _applicationStateService.CurrentUser?.Role?.RoleName;
            if (string.IsNullOrEmpty(currentUserRoleName)) return false;
            return _permissionService.HasPermission(Permissions.ManageUsers, currentUserRoleName) && !IsBusy;
        }

        [RelayCommand(CanExecute = nameof(CanAddUser))]
        private async Task AddUser()
        {
            _logger.LogInformation("AddUser command executed.");
            var addEditUserViewModel = _serviceProvider.GetRequiredService<AddEditUserViewModel>();

            // CORRECTED: Initialize AddEditUserViewModel for a new user.
            // LoadAsync(null) should handle loading roles within AddEditUserViewModel.
            await addEditUserViewModel.LoadAsync(null);

            var result = await _dialogService.ShowDialogAsync(addEditUserViewModel, "Добавить пользователя");
            if (result == true)
            {
                _logger.LogInformation("User addition dialog returned success.");
                // Message should update or reload users.
            }
        }

        // CORRECTED: Combined CanExecute logic with permission check, including roleName
        private bool CanEditDeleteOrToggleUser()
        {
            var currentUserRoleName = _applicationStateService.CurrentUser?.Role?.RoleName;
            if (string.IsNullOrEmpty(currentUserRoleName) || SelectedUser == null) return false;
            return _permissionService.HasPermission(Permissions.ManageUsers, currentUserRoleName) && !IsBusy;
        }

        [RelayCommand(CanExecute = nameof(CanEditDeleteOrToggleUser))]
        private async Task EditUser()
        {
            if (SelectedUser == null) return;
            _logger.LogInformation("EditUser command executed for user ID: {UserId}", SelectedUser.UserId);

            var editUserViewModel = _serviceProvider.GetRequiredService<AddEditUserViewModel>();

            // This line was already correctly using LoadAsync as per my previous suggestions.
            await editUserViewModel.LoadAsync(SelectedUser);

            var dialogResult = await _dialogService.ShowDialogAsync(editUserViewModel, $"Редактировать: {SelectedUser.Username}");

            if (dialogResult == true)
            {
                _logger.LogInformation("User edit dialog returned success for user ID: {UserId}", SelectedUser.UserId);
            }
            else
            {
                _logger.LogInformation("User edit dialog was cancelled or closed for user ID: {UserId}", SelectedUser.UserId);
            }
        }

        [RelayCommand(CanExecute = nameof(CanEditDeleteOrToggleUser))]
        private async Task DeleteUser()
        {
            if (SelectedUser == null) return;

            // Assuming IDialogService has ShowConfirmationAsync (as it should from previous fixes)
            bool confirm = await _dialogService.ShowConfirmAsync("Удалить пользователя", $"Вы уверены, что хотите удалить пользователя '{SelectedUser.Username}'?");
            if (!confirm)
            {
                _logger.LogInformation("Deletion cancelled for user ID: {UserId}", SelectedUser.UserId);
                return;
            }

            _logger.LogInformation("Attempting to delete user ID: {UserId}", SelectedUser.UserId);
            IsBusy = true;
            try
            {
                await _userService.DeleteUserAsync(SelectedUser.UserId);
                _logger.LogInformation("User ID: {UserId} marked as inactive (deleted).", SelectedUser.UserId);

                // Assuming IDialogService has ShowMessage (as it should)
                _dialogService.ShowMessage("Успех", $"Пользователь '{SelectedUser.Username}' успешно помечен как неактивный.");

                await LoadUsersAsync();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operation to delete user ID {UserId} was invalid: {ErrorMessage}", SelectedUser.UserId, ex.Message);
                // Assuming IDialogService has ShowWarning (as it should from previous fixes)
                _dialogService.ShowWarning("Операция запрещена", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user ID: {UserId}.", SelectedUser.UserId);
                _dialogService.ShowError("Ошибка", "Произошла непредвиденная ошибка при удалении пользователя.", ex.ToString());
            }
            finally
            {
                IsBusy = false;
                AddUserCommand.NotifyCanExecuteChanged();
                EditUserCommand.NotifyCanExecuteChanged();
                DeleteUserCommand.NotifyCanExecuteChanged();
                ToggleUserStatusCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanEditDeleteOrToggleUser))]
        private async Task ToggleUserStatus()
        {
            if (SelectedUser == null) return;

            string action = SelectedUser.IsActive ? "деактивировать" : "активировать";
            bool confirm = await _dialogService.ShowConfirmAsync("Подтвердить действие", $"Вы уверены, что хотите {action} пользователя '{SelectedUser.Username}'?");
            if (!confirm) return;

            _logger.LogInformation("Attempting to {Action} user ID: {UserId}", action, SelectedUser.UserId);
            IsBusy = true;
            try
            {
                bool success = false; // Initialize success
                if (SelectedUser.IsActive)
                {
                    await _userService.DeleteUserAsync(SelectedUser.UserId);
                    success = true;
                }
                else
                {
                    success = await _userService.ActivateUserAsync(SelectedUser.UserId);
                }

                if (success)
                {
                    // SelectedUser.IsActive = !SelectedUser.IsActive; // State will be updated by LoadUsersAsync
                    _logger.LogInformation("User {Username} status change process initiated.", SelectedUser.Username);
                    _dialogService.ShowMessage("Успех", $"Статус пользователя '{SelectedUser.Username}' изменен.");
                    await LoadUsersAsync(); // Reload to get the updated state consistently
                }
                else
                {
                    // This block might not be reached if services throw exceptions on failure.
                    _dialogService.ShowError("Ошибка", $"Не удалось {action} пользователя.", "Операция в сервисе вернула false или не выполнилась.");
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operation to {Action} user ID {UserId} was invalid: {ErrorMessage}", action, SelectedUser.UserId, ex.Message);
                _dialogService.ShowWarning("Операция запрещена", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status for {Username}", SelectedUser.Username);
                _dialogService.ShowError("Ошибка", "Произошла непредвиденная ошибка.", ex.ToString());
            }
            finally
            {
                IsBusy = false;
                AddUserCommand.NotifyCanExecuteChanged();
                EditUserCommand.NotifyCanExecuteChanged();
                DeleteUserCommand.NotifyCanExecuteChanged();
                ToggleUserStatusCommand.NotifyCanExecuteChanged();
            }
        }

        public void Receive(UserAddedMessage message)
        {
            _logger.LogInformation("UserAddedMessage received for user: {Username}", message.Value.Username);
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadUsersAsync();
                _logger.LogInformation("Users list reloaded after UserAddedMessage for: {Username}.", message.Value.Username);
            });
        }

        public void Receive(UserUpdatedMessage message)
        {
            _logger.LogInformation("UserUpdatedMessage received for user: {Username}", message.Value.Username);
            Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadUsersAsync();
                _logger.LogInformation("Users list reloaded after UserUpdatedMessage for: {Username}.", message.Value.Username);
            });
        }
    }
}