// WpfApp1/ViewModels/AddEditDeviceViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using WpfApp1.Commands;
using WpfApp1.Interfaces;
using WpfApp1.Models;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public class AddEditDeviceViewModel : BaseViewModel, IDialogViewModel
    {
        private readonly IDeviceService _deviceService;
        private readonly IDialogService _dialogService;
        private readonly IApplicationStateService _applicationStateService;
        private readonly IUserService _userService;
        private readonly ILogger<AddEditDeviceViewModel> _logger;

        private Device _currentDevice;
        public Device CurrentDevice
        {
            get => _currentDevice;
            set
            {
                if (SetProperty(ref _currentDevice, value))
                {
                    if (IsEditing && _currentDevice != null)
                    {
                        // Corrected property access for DeviceType and DeviceStatus
                        SelectedDeviceType = DeviceTypes?.FirstOrDefault(dt => dt.DeviceTypeId == _currentDevice.DeviceTypeId); // Use DeviceTypeId
                        SelectedDeviceStatus = DeviceStatuses?.FirstOrDefault(ds => ds.StatusId == _currentDevice.StatusId);       // Use StatusId
                        SelectedResponsibleUser = AvailableUsers?.FirstOrDefault(u => u.UserId == _currentDevice.ResponsibleUserId); // Use ResponsibleUserId
                    }
                }
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        // Title is part of IDialogViewModel and is set in the constructor or an init method
        // public string Title { get; private set; } // This is inherited/managed by BaseViewModel or set by IDialogViewModel host

        // --- IDialogViewModel Implementation ---
        private bool? _dialogResult;
        public bool? DialogResult => _dialogResult;

        public event Action<bool?>? CloseRequested; // Corrected event signature

        public void RequestClose(bool? result)
        {
            _dialogResult = result;
            CloseRequested?.Invoke(_dialogResult);
        }
        // --- End IDialogViewModel Implementation ---

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private ObservableCollection<DeviceType> _deviceTypes;
        public ObservableCollection<DeviceType> DeviceTypes
        {
            get => _deviceTypes;
            set => SetProperty(ref _deviceTypes, value);
        }

        private ObservableCollection<DeviceStatus> _deviceStatuses;
        public ObservableCollection<DeviceStatus> DeviceStatuses
        {
            get => _deviceStatuses;
            set => SetProperty(ref _deviceStatuses, value);
        }

        private ObservableCollection<User> _availableUsers;
        public ObservableCollection<User> AvailableUsers
        {
            get => _availableUsers;
            set => SetProperty(ref _availableUsers, value);
        }

        private DeviceType _selectedDeviceType;
        public DeviceType SelectedDeviceType
        {
            get => _selectedDeviceType;
            set
            {
                // Corrected property access for DeviceType
                if (SetProperty(ref _selectedDeviceType, value) && CurrentDevice != null)
                {
                    CurrentDevice.DeviceTypeId = value?.DeviceTypeId ?? 0; // Use DeviceTypeId
                    CurrentDevice.DeviceType = value;
                }
            }
        }

        private DeviceStatus _selectedDeviceStatus;
        public DeviceStatus SelectedDeviceStatus
        {
            get => _selectedDeviceStatus;
            set
            {
                // Исправлен доступ к свойству для DeviceStatus
                if (SetProperty(ref _selectedDeviceStatus, value) && CurrentDevice != null)
                {
                    // Исправленная строка 113:
                    CurrentDevice.StatusId = value?.StatusId ?? 0; // Использовать StatusId и предоставить значение по умолчанию, если null
                    CurrentDevice.DeviceStatus = value;
                }
            }
        }

        private User _selectedResponsibleUser;
        public User SelectedResponsibleUser
        {
            get => _selectedResponsibleUser;
            set
            {
                // Corrected property access for User
                if (SetProperty(ref _selectedResponsibleUser, value) && CurrentDevice != null)
                {
                    CurrentDevice.ResponsibleUserId = value?.UserId; // Use ResponsibleUserId
                    CurrentDevice.ResponsibleUser = value;          // Use ResponsibleUser
                }
            }
        }

        // Constructor for runtime
        public AddEditDeviceViewModel(
            IDeviceService deviceService,
            IDialogService dialogService,
            IApplicationStateService applicationStateService,
            IUserService userService,
            ILogger<AddEditDeviceViewModel> logger,
            Device? deviceToEdit = null) // deviceToEdit should be nullable
        {
            _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);

            if (deviceToEdit == null)
            {
                Title = "Добавить новое устройство";
                CurrentDevice = new Device { /* PurchaseDate = DateTime.Today, LastMaintenanceDate = DateTime.Today, WarrantyEndDate = DateTime.Today.AddYears(1) */ };
                IsEditing = false;
                _logger.LogInformation("AddEditDeviceViewModel initialized for adding a new device.");
            }
            else
            {
                Title = $"Редактировать устройство: {deviceToEdit.Name}";
                CurrentDevice = deviceToEdit; // This will trigger the setter and update Selected properties if IsEditing is true
                IsEditing = true;
                _logger.LogInformation("AddEditDeviceViewModel initialized for editing device ID: {DeviceId}", deviceToEdit.DeviceId);
            }
            // LoadRelatedData should be called after CurrentDevice and IsEditing are set.
            LoadRelatedData();
        }

        // Constructor without parameters for XAML designer (if needed)
        public AddEditDeviceViewModel()
        {
            Title = "Добавить/Редактировать Устройство (Дизайн)";
            CurrentDevice = new Device
            {
                Name = "Тестовое имя",
                InventoryNumber = "INV-123",
                // ... other design-time properties
            };

            DeviceTypes = new ObservableCollection<DeviceType>
            {
                new DeviceType { DeviceTypeId = 1, Name = "Компьютер (Дизайн)" },
                new DeviceType { DeviceTypeId = 2, Name = "Принтер (Дизайн)" }
            };
            if (DeviceTypes.Any()) SelectedDeviceType = DeviceTypes.First();


            DeviceStatuses = new ObservableCollection<DeviceStatus>
            {
                new DeviceStatus { StatusId = 1, StatusName = "В работе (Дизайн)" },
                new DeviceStatus { StatusId = 2, StatusName = "В ремонте (Дизайн)" }
            };
            if (DeviceStatuses.Any()) SelectedDeviceStatus = DeviceStatuses.First();

            AvailableUsers = new ObservableCollection<User>
            {
                new User { UserId = 1, FullName = "Иван Иванов (Дизайн)"},
                new User { UserId = 2, FullName = "Петр Петров (Дизайн)"}
            };
            if (AvailableUsers.Any()) SelectedResponsibleUser = AvailableUsers.First();

            SaveCommand = new RelayCommand(() => { }, () => true); // Dummy commands for designer
            CancelCommand = new RelayCommand(() => { });
        }


        private async void LoadRelatedData()
        {
            if (_deviceService == null || _userService == null)
            {
                _logger?.LogWarning("Services not available in design mode or due to DI issue. Skipping LoadRelatedData.");
                return;
            }

            _logger.LogInformation("Attempting to load related data...");
            try
            {
                var types = await _deviceService.GetAllDeviceTypesAsync();
                DeviceTypes = new ObservableCollection<DeviceType>(types);
                _logger.LogInformation("Loaded {Count} device types.", DeviceTypes?.Count ?? 0);

                var statuses = await _deviceService.GetAllDeviceStatusesAsync();
                DeviceStatuses = new ObservableCollection<DeviceStatus>(statuses);
                _logger.LogInformation("Loaded {Count} device statuses.", DeviceStatuses?.Count ?? 0);

                var users = await _userService.GetAllUsersAsync(); // Assuming GetAllUsersAsync exists
                AvailableUsers = new ObservableCollection<User>(users);
                _logger.LogInformation("Loaded {Count} available users.", AvailableUsers?.Count ?? 0);


                if (IsEditing && CurrentDevice != null)
                {
                    // Corrected property access
                    SelectedDeviceType = DeviceTypes?.FirstOrDefault(dt => dt.DeviceTypeId == CurrentDevice.DeviceTypeId);
                    SelectedDeviceStatus = DeviceStatuses?.FirstOrDefault(ds => ds.StatusId == CurrentDevice.StatusId);
                    SelectedResponsibleUser = AvailableUsers?.FirstOrDefault(u => u.UserId == CurrentDevice.ResponsibleUserId);
                }
                else if (!IsEditing)
                {
                    SelectedDeviceType = DeviceTypes?.FirstOrDefault();
                    SelectedDeviceStatus = DeviceStatuses?.FirstOrDefault();
                    // SelectedResponsibleUser = AvailableUsers?.FirstOrDefault(); // Or null by default
                }
                _logger.LogInformation("Finished loading related data. Selected Type: {SelectedType}, Status: {SelectedStatus}, User: {SelectedUser}", SelectedDeviceType?.Name, SelectedDeviceStatus?.StatusName, SelectedResponsibleUser?.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading related data (types/statuses/users) for AddEditDeviceViewModel.");
                _dialogService?.ShowError("Ошибка загрузки", $"Не удалось загрузить связанные данные: {ex.Message}", ex.ToString());
            }
        }

        private bool CanSave()
        {
            bool isValid = CurrentDevice != null &&
                           !string.IsNullOrWhiteSpace(CurrentDevice.Name) &&
                           SelectedDeviceType != null &&
                           SelectedDeviceStatus != null; // Status is now likely required

            if (!isValid) _logger?.LogWarning("CanSave is false. Name: {Name}, Type: {Type}, Status: {Status}", CurrentDevice?.Name, SelectedDeviceType?.Name, SelectedDeviceStatus?.StatusName);
            return isValid;
        }

        private async void Save()
        {
            _logger.LogInformation("SaveCommand executed for device: {DeviceName}", CurrentDevice?.Name);
            if (!CanSave())
            {
                _logger.LogWarning("Save aborted due to validation errors (checked in Save method).");
                _dialogService.ShowError("Ошибка валидации", "Пожалуйста, заполните все обязательные поля.", "");
                return;
            }

            try
            {
                if (IsEditing)
                {
                    await _deviceService.UpdateDeviceAsync(CurrentDevice);
                    _logger.LogInformation("Device ID {DeviceId} updated.", CurrentDevice.DeviceId);
                }
                else
                {
                    await _deviceService.AddDeviceAsync(CurrentDevice);
                    _logger.LogInformation("New device {DeviceName} added with ID {DeviceId}.", CurrentDevice.Name, CurrentDevice.DeviceId);
                }
                RequestClose(true); // Use the new method
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving device: {DeviceName}", CurrentDevice?.Name);
                _dialogService.ShowError("Ошибка сохранения", $"Не удалось сохранить устройство: {ex.Message}", ex.ToString());
            }
        }

        private void Cancel()
        {
            _logger.LogInformation("CancelCommand executed.");
            RequestClose(false); // Use the new method
        }
    }
}