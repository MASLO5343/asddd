// WpfApp1/ViewModels/AddEditUserViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WpfApp1.Interfaces;
using WpfApp1.Messages;
using WpfApp1.Models;
// DialogCloseRequestedEventArgs is likely no longer needed here if CloseRequested event changes signature
// using WpfApp1.Services; // If DialogCloseRequestedEventArgs was there

namespace WpfApp1.ViewModels
{
    public partial class AddEditUserViewModel : BaseViewModel, IDialogViewModel
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<AddEditUserViewModel> _logger;
        private readonly IMessenger _messenger;

        private User? _editingUser;

        [ObservableProperty]
        private string _title = "User Details";

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _firstName = string.Empty;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string? _password;

        [ObservableProperty]
        private string? _confirmPassword;

        [ObservableProperty]
        private Role? _selectedRole;

        [ObservableProperty]
        private bool _isActive;

        [ObservableProperty]
        private ObservableCollection<Role> _roles = new();

        [ObservableProperty]
        private bool _isEditing;

        public User? SavedUser { get; private set; }

        // --- IDialogViewModel Implementation ---
        public event Action<bool?>? CloseRequested; // Corrected event signature to match IDialogViewModel

        private bool? _dialogResultInternal;
        public bool? DialogResult => _dialogResultInternal;

        public void RequestClose(bool? result)
        {
            _dialogResultInternal = result;
            CloseRequested?.Invoke(result); // Invoke the Action<bool?> event
        }
        // --- End IDialogViewModel Implementation ---

        public AddEditUserViewModel(
            IUserService userService,
            IRoleService roleService,
            IDialogService dialogService,
            ILogger<AddEditUserViewModel> logger,
            IMessenger messenger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messenger = messenger;
        }

        public AddEditUserViewModel()
        {
            _logger = new LoggerFactory().CreateLogger<AddEditUserViewModel>();
            Title = "Add/Edit User (Design)";
            Username = "designer_user";
            FirstName = "Design";
            LastName = "User";
            IsActive = true;
            Roles = new ObservableCollection<Role> { new Role { RoleId = 1, RoleName = "Admin (Design)" }, new Role { RoleId = 2, RoleName = "User (Design)" } };
            SelectedRole = Roles.FirstOrDefault();
            IsEditing = false;
        }

        public async Task LoadAsync(User? userToEdit = null)
        {
            await LoadRolesAsync();
            InitializeUser(userToEdit);
        }

        public async Task LoadRolesAsync()
        {
            if (_roleService == null) return;
            IsBusy = true;
            try
            {
                _logger.LogInformation("Loading roles for AddEditUserViewModel.");
                var rolesList = await _roleService.GetAllRolesAsync();
                Roles.Clear();
                foreach (var role in rolesList)
                {
                    Roles.Add(role);
                }
                _logger.LogInformation("Successfully loaded {Count} roles.", Roles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading roles.");
                _dialogService?.ShowError("Error Loading Roles", $"Failed to load roles: {ex.Message}", ex.ToString());
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void InitializeUser(User? user)
        {
            _editingUser = user;
            SavedUser = null;
            if (user != null)
            {
                IsEditing = true;
                Title = $"Edit User: {user.Username}";
                Username = user.Username;

                if (!string.IsNullOrEmpty(user.FullName))
                {
                    var nameParts = user.FullName.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    FirstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
                    LastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
                }
                else
                {
                    FirstName = string.Empty;
                    LastName = string.Empty;
                }

                IsActive = user.IsActive;
                Password = null;
                ConfirmPassword = null;

                // Corrected lines for CS1061: user.RoleId is int, not int?
                // No .HasValue or .Value needed. Assuming RoleId > 0 means a role is assigned.
                if (Roles.Any() && user.RoleId > 0) // Line 158 (approx.)
                {
                    SelectedRole = Roles.FirstOrDefault(r => r.RoleId == user.RoleId); // Line 160 (approx.)
                }
                else if (!Roles.Any())
                {
                    _logger.LogWarning("Roles not loaded when initializing user. SelectedRole might not be set for user {Username}.", Username);
                }
                _logger.LogInformation("Initialized ViewModel for editing user: {Username}.", Username);
            }
            else
            {
                IsEditing = false;
                Title = "Add New User";
                Username = string.Empty;
                FirstName = string.Empty;
                LastName = string.Empty;
                Password = string.Empty;
                ConfirmPassword = string.Empty;
                IsActive = true;
                SelectedRole = Roles.FirstOrDefault();
                _logger.LogInformation("Initialized ViewModel for adding a new user.");
            }
        }

        [RelayCommand]
        private async Task SaveUserAsync()
        {
            _logger.LogInformation("Attempting to save user: {Username}", Username);
            if (!await ValidateUserInputAsync())
            {
                _logger.LogWarning("User input validation failed for user: {Username}", Username);
                return;
            }

            IsBusy = true;
            try
            {
                User userToSave;
                bool isNewUser = !IsEditing || _editingUser == null;

                if (isNewUser)
                {
                    userToSave = new User();
                }
                else
                {
                    userToSave = _editingUser!;
                }

                userToSave.Username = Username;
                userToSave.FullName = $"{FirstName} {LastName}".Trim();
                userToSave.IsActive = IsActive;
                userToSave.RoleId = SelectedRole!.RoleId;

                bool overallSuccess = false;
                string? operationError = null;

                if (isNewUser)
                {
                    _logger.LogInformation("Creating new user: {Username}", Username);
                    User? createdUser = await _userService.CreateUserAsync(userToSave, Password!);
                    if (createdUser != null)
                    {
                        userToSave = createdUser;
                        overallSuccess = true;
                        _logger.LogInformation("User {Username} created successfully with ID {UserId}.", userToSave.Username, userToSave.UserId);
                    }
                    else
                    {
                        operationError = "Failed to create the new user.";
                        _logger.LogError("User creation failed for {Username}.", Username);
                    }
                }
                else
                {
                    _logger.LogInformation("Updating existing user: {Username} (ID: {UserId})", userToSave.Username, userToSave.UserId);
                    bool detailsUpdated = await _userService.UpdateUserAsync(userToSave);
                    if (detailsUpdated)
                    {
                        overallSuccess = true;
                        _logger.LogInformation("User details for {Username} updated successfully.", userToSave.Username);

                        if (!string.IsNullOrWhiteSpace(Password))
                        {
                            _logger.LogInformation("Attempting to change password for user: {Username}", userToSave.Username);
                            bool passwordChanged = await _userService.ChangePasswordAsync(userToSave.UserId, Password!);
                            if (passwordChanged)
                            {
                                _logger.LogInformation("Password for user {Username} changed successfully.", userToSave.Username);
                            }
                            else
                            {
                                overallSuccess = false;
                                operationError = "User details updated, but failed to change the password.";
                                _logger.LogError("Password change failed for user: {Username}", userToSave.Username);
                            }
                        }
                    }
                    else
                    {
                        operationError = "Failed to update user details.";
                        _logger.LogError("User details update failed for {Username}.", userToSave.Username);
                    }
                }

                if (overallSuccess)
                {
                    _logger.LogInformation("User {Username} operation completed successfully.", Username);
                    SavedUser = userToSave;
                    _dialogService.ShowMessage("Success", $"User '{Username}' has been saved successfully.");

                    if (isNewUser)
                    {
                        _messenger?.Send(new UserAddedMessage(userToSave));
                    }
                    else
                    {
                        _messenger?.Send(new UserUpdatedMessage(userToSave));
                    }
                    RequestClose(true); // Use the new method from IDialogViewModel
                }
                else
                {
                    string fullErrorMessage = $"Could not save user '{Username}'.";
                    if (!string.IsNullOrEmpty(operationError))
                    {
                        fullErrorMessage += $" Reason: {operationError}";
                    }
                    else
                    {
                        fullErrorMessage += " An unknown error occurred.";
                    }
                    _logger.LogError("Failed to save user {Username}. Error: {Error}", Username, operationError ?? "Unknown error.");
                    _dialogService.ShowError("Save Failed", fullErrorMessage, "");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred while saving user {Username}.", Username);
                _dialogService.ShowError("Error", $"An unexpected error occurred while saving the user: {ex.Message}", ex.ToString());
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _logger.LogInformation("User edit/add cancelled for user: {Username}.", Username);
            SavedUser = null;
            RequestClose(false); // Use the new method from IDialogViewModel
        }

        private async Task<bool> ValidateUserInputAsync()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                _dialogService.ShowMessage("Validation Error", "Username cannot be empty.");
                return false;
            }

            if (!IsEditing || (_editingUser != null && _editingUser.Username != Username))
            {
                var existingUser = await _userService.GetUserByUsernameAsync(Username);
                if (existingUser != null)
                {
                    _dialogService.ShowMessage("Validation Error", $"Username '{Username}' already exists.");
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(FirstName))
            {
                _dialogService.ShowMessage("Validation Error", "First name cannot be empty.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(LastName))
            {
                _dialogService.ShowMessage("Validation Error", "Last name cannot be empty.");
                return false;
            }

            bool isNewUser = !IsEditing || _editingUser == null;

            if (isNewUser)
            {
                if (string.IsNullOrWhiteSpace(Password))
                {
                    _dialogService.ShowMessage("Validation Error", "Password cannot be empty for a new user.");
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(Password))
            {
                if (Password != ConfirmPassword)
                {
                    _dialogService.ShowMessage("Validation Error", "Passwords do not match.");
                    return false;
                }
                if (Password.Length < 6)
                {
                    _dialogService.ShowMessage("Validation Error", "Password must be at least 6 characters long.");
                    return false;
                }
            }

            if (SelectedRole == null)
            {
                _dialogService.ShowMessage("Validation Error", "A role must be selected.");
                return false;
            }

            _logger.LogInformation("User input validation successful for {Username}.", Username);
            return true;
        }
    }
}