// File: WpfApp1/ViewModels/InventoryViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WpfApp1.Models;
using WpfApp1.Services; // Ensure this is WpfApp1.Services for IDialogService etc.
using WpfApp1.Interfaces;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
// Removed: using WpfApp1.Services.Data; // Not needed if DeviceFilterParameters is in Models or WpfApp1.Services directly

namespace WpfApp1.ViewModels
{
    public partial class InventoryViewModel : BaseViewModel
    {
        private readonly IDeviceService _deviceService;
        private readonly ISoftwareService _softwareService;
        private readonly IDeviceSoftwareService _deviceSoftwareService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<InventoryViewModel> _logger;
        private readonly IApplicationStateService _applicationStateService; // ADDED: Dependency
        private readonly IUserService _userService; // ADDED: Dependency
        private readonly ILoggerFactory _loggerFactory; // ADDED: Dependency for creating other loggers

        public InventoryViewModel(
            IDeviceService deviceService,
            ISoftwareService softwareService,
            IDeviceSoftwareService deviceSoftwareService,
            IDialogService dialogService,
            ILogger<InventoryViewModel> logger,
            IApplicationStateService applicationStateService, // ADDED: Constructor parameter
            IUserService userService, // ADDED: Constructor parameter
            ILoggerFactory loggerFactory) // ADDED: Constructor parameter
        {
            _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
            _softwareService = softwareService ?? throw new ArgumentNullException(nameof(softwareService));
            _deviceSoftwareService = deviceSoftwareService ?? throw new ArgumentNullException(nameof(deviceSoftwareService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService)); // ADDED: Assignment
            _userService = userService ?? throw new ArgumentNullException(nameof(userService)); // ADDED: Assignment
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)); // ADDED: Assignment

            Title = "Инвентаризация"; // Set title for the page

            Devices = new ObservableCollection<Device>();
            SelectedDeviceInstalledSoftware = new ObservableCollection<DeviceSoftware>();

            // Initialize Commands
            LoadDevicesCommand = new AsyncRelayCommand(LoadDevicesAsync, CanLoadDevices); // ADDED: Initialize LoadDevicesCommand
            AddDeviceCommand = new AsyncRelayCommand(ExecuteAddDeviceCommand);
            EditDeviceCommand = new AsyncRelayCommand<Device>(async (device) => await ExecuteEditDeviceCommand(device ?? SelectedDevice), (device) => (device ?? SelectedDevice) != null);
            DeleteDeviceCommand = new AsyncRelayCommand<Device>(async (device) => await ExecuteDeleteDeviceCommand(device ?? SelectedDevice), (device) => (device ?? SelectedDevice) != null);
            SortCommand = new RelayCommand<string>(ExecuteSortCommand);

            AddSoftwareToDeviceCommand = new AsyncRelayCommand(ExecuteAddSoftwareToDeviceCommand, CanExecuteAddSoftwareToDevice);
            RemoveSoftwareFromDeviceCommand = new AsyncRelayCommand<DeviceSoftware>(ExecuteRemoveSoftwareFromDeviceCommand, CanExecuteRemoveSoftwareFromDevice);

            // Initialize filter collections
            AvailableDeviceTypes = new ObservableCollection<DeviceType>();
            AvailableDeviceStatuses = new ObservableCollection<DeviceStatus>();
            AvailableDepartments = new ObservableCollection<string>();
            AvailableResponsibleUsers = new ObservableCollection<User>();
            AvailableLocations = new ObservableCollection<string>();
        }

        // Properties for Devices
        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private ObservableCollection<Device> _devices;

        private Device? _selectedDevice;
        public Device? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value))
                {
                    AddSoftwareToDeviceCommand.NotifyCanExecuteChanged();
                    RemoveSoftwareFromDeviceCommand.NotifyCanExecuteChanged(); // Update this too
                    _ = LoadInstalledSoftwareAsync(); // Async void is acceptable for event-like handlers
                    EditDeviceCommand.NotifyCanExecuteChanged(); // Update dependent commands
                    DeleteDeviceCommand.NotifyCanExecuteChanged();
                }
            }
        }

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadDevicesCommand))]
        private string _searchText = string.Empty;

        // Properties for Filtering (Generated by CommunityToolkit.Mvvm)
        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadDevicesCommand))]
        private DeviceType? _selectedDeviceTypeFilter;

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadDevicesCommand))]
        private DeviceStatus? _selectedDeviceStatusFilter;

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadDevicesCommand))]
        private string? _selectedDepartmentFilter;

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadDevicesCommand))]
        private User? _selectedResponsibleUserFilter;

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadDevicesCommand))]
        private string? _selectedLocationFilter;

        public ObservableCollection<DeviceType> AvailableDeviceTypes { get; }
        public ObservableCollection<DeviceStatus> AvailableDeviceStatuses { get; }
        public ObservableCollection<string> AvailableDepartments { get; }
        public ObservableCollection<User> AvailableResponsibleUsers { get; }
        public ObservableCollection<string> AvailableLocations { get; }


        // Properties for Sorting
        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadDevicesCommand))] // MODIFIED: Assuming sort change should re-trigger CanExecute for LoadDevices if applicable
        private string _sortColumn = "Name";

        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoadDevicesCommand))] // MODIFIED: Assuming sort change should re-trigger CanExecute for LoadDevices if applicable
        private bool _isSortAscending = true;

        // Properties for Installed Software on Selected Device
        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private ObservableCollection<DeviceSoftware> _selectedDeviceInstalledSoftware;

        private DeviceSoftware? _selectedInstalledSoftwareItem;
        public DeviceSoftware? SelectedInstalledSoftwareItem
        {
            get => _selectedInstalledSoftwareItem;
            set
            {
                if (SetProperty(ref _selectedInstalledSoftwareItem, value))
                {
                    RemoveSoftwareFromDeviceCommand.NotifyCanExecuteChanged();
                }
            }
        }


        [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
        private bool _isLoadingInstalledSoftware;


        // Commands
        public IAsyncRelayCommand LoadDevicesCommand { get; } // ADDED: Command Property
        public IAsyncRelayCommand AddDeviceCommand { get; }
        public IAsyncRelayCommand<Device> EditDeviceCommand { get; }
        public IAsyncRelayCommand<Device> DeleteDeviceCommand { get; }
        public IRelayCommand<string> SortCommand { get; }
        public IAsyncRelayCommand AddSoftwareToDeviceCommand { get; }
        public IAsyncRelayCommand<DeviceSoftware> RemoveSoftwareFromDeviceCommand { get; }


        // Lifecycle and Data Loading
        public override async Task OnNavigatedTo(object parameter)
        {
            await LoadInitialDataAsync();
            await base.OnNavigatedTo(parameter);
        }

        private async Task LoadInitialDataAsync()
        {
            IsBusy = true;
            _logger.LogInformation("InventoryViewModel: Loading initial data...");
            try
            {
                await LoadFilterCollectionsAsync();
                await LoadDevicesAsync(); // This will be executed by the LoadDevicesCommand initially if needed, or directly.
                                          // If LoadDevicesCommand has a CanExecute that depends on IsBusy, direct call is fine here.
                SelectedDeviceInstalledSoftware.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InventoryViewModel: Error loading initial data.");
                await _dialogService.ShowErrorAsync("Ошибка загрузки", "Не удалось загрузить начальные данные для инвентаризации.", ex.ToString());
            }
            finally
            {
                IsBusy = false;
                LoadDevicesCommand.NotifyCanExecuteChanged(); // Ensure command state is updated after IsBusy changes
                _logger.LogInformation("InventoryViewModel: Initial data loading complete.");
            }
        }

        private async Task LoadFilterCollectionsAsync()
        {
            _logger.LogInformation("Loading filter collections...");
            try
            {
                var deviceTypesTask = _deviceService.GetAllDeviceTypesAsync();
                var deviceStatusesTask = _deviceService.GetAllDeviceStatusesAsync();
                var departmentsTask = _deviceService.GetAvailableDepartmentsAsync();
                var responsibleUsersTask = _deviceService.GetAvailableResponsibleUsersAsync();
                var locationsTask = _deviceService.GetAvailableLocationsAsync();

                await Task.WhenAll(deviceTypesTask, deviceStatusesTask, departmentsTask, responsibleUsersTask, locationsTask);

                AvailableDeviceTypes.Clear();
                foreach (var type in (await deviceTypesTask).OrderBy(t => t.Name)) AvailableDeviceTypes.Add(type);

                AvailableDeviceStatuses.Clear();
                foreach (var status in (await deviceStatusesTask).OrderBy(s => s.StatusName)) AvailableDeviceStatuses.Add(status);

                AvailableDepartments.Clear();
                foreach (var dep in (await departmentsTask).OrderBy(d => d)) AvailableDepartments.Add(dep);

                AvailableResponsibleUsers.Clear();
                foreach (var user in (await responsibleUsersTask).OrderBy(u => u.FullName)) AvailableResponsibleUsers.Add(user);

                AvailableLocations.Clear();
                foreach (var loc in (await locationsTask).OrderBy(l => l)) AvailableLocations.Add(loc);

                _logger.LogInformation("Filter collections loaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading filter collections.");
                await _dialogService.ShowErrorAsync("Ошибка", "Не удалось загрузить данные для фильтров.", ex.ToString());
            }
        }

        private bool CanLoadDevices() => !IsBusy; // ADDED: CanExecute for LoadDevicesCommand

        // MODIFIED: Made public to be accessible by IAsyncRelayCommand if it wasn't already (though it was private and called directly too)
        public async Task LoadDevicesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            LoadDevicesCommand.NotifyCanExecuteChanged(); // Update CanExecute state
            _logger.LogInformation("Loading devices with current filters and sort order...");

            try
            {
                var filterParameters = new WpfApp1.Services.Data.DeviceFilterParameters
                {
                    SearchTerm = SearchText,
                    TypeId = SelectedDeviceTypeFilter?.DeviceTypeId,
                    StatusId = SelectedDeviceStatusFilter?.StatusId,
                    Department = string.IsNullOrWhiteSpace(SelectedDepartmentFilter) ? null : SelectedDepartmentFilter,
                    ResponsibleUserId = SelectedResponsibleUserFilter?.UserId,
                    Location = string.IsNullOrWhiteSpace(SelectedLocationFilter) ? null : SelectedLocationFilter,
                };

                var devicesList = await _deviceService.GetDevicesAsync(filterParameters, SortColumn, IsSortAscending);
                Devices.Clear();
                foreach (var device in devicesList)
                {
                    Devices.Add(device);
                }
                _logger.LogInformation($"Loaded {Devices.Count} devices.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading devices.");
                await _dialogService.ShowErrorAsync("Ошибка", "Не удалось загрузить список устройств.", ex.ToString());
            }
            finally
            {
                IsBusy = false;
                LoadDevicesCommand.NotifyCanExecuteChanged(); // Update CanExecute state
            }
        }


        private void ExecuteSortCommand(string? columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return;

            _logger.LogInformation($"Sorting by column: {columnName}. Current sort: {SortColumn} {(IsSortAscending ? "ASC" : "DESC")}");
            if (SortColumn == columnName)
            {
                IsSortAscending = !IsSortAscending;
            }
            else
            {
                SortColumn = columnName;
                IsSortAscending = true;
            }
            _logger.LogInformation($"New sort: {SortColumn} {(IsSortAscending ? "ASC" : "DESC")}");
            // Instead of direct call, consider if LoadDevicesCommand should be invoked if it's now executable.
            // Direct call is fine if LoadDevicesAsync handles its own IsBusy check.
            _ = LoadDevicesAsync();
        }

        private async Task LoadInstalledSoftwareAsync()
        {
            if (SelectedDevice == null)
            {
                SelectedDeviceInstalledSoftware.Clear();
                SelectedInstalledSoftwareItem = null;
                return;
            }

            IsLoadingInstalledSoftware = true;
            SelectedInstalledSoftwareItem = null;
            AddSoftwareToDeviceCommand.NotifyCanExecuteChanged(); // Reflect change in IsLoadingInstalledSoftware
            RemoveSoftwareFromDeviceCommand.NotifyCanExecuteChanged();

            _logger.LogInformation("Loading installed software for Device ID: {DeviceId}", SelectedDevice.DeviceId);
            try
            {
                var installedSoftwareList = await _deviceSoftwareService.GetSoftwareForDeviceAsync(SelectedDevice.DeviceId);
                SelectedDeviceInstalledSoftware.Clear();
                foreach (var item in installedSoftwareList)
                {
                    SelectedDeviceInstalledSoftware.Add(item);
                }
                _logger.LogInformation("Loaded {Count} installed software items for Device ID: {DeviceId}", SelectedDeviceInstalledSoftware.Count, SelectedDevice.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading installed software for Device ID: {DeviceId}", SelectedDevice.DeviceId);
                await _dialogService.ShowErrorAsync("Ошибка", "Не удалось загрузить список ПО для устройства.", ex.ToString());
                SelectedDeviceInstalledSoftware.Clear();
            }
            finally
            {
                IsLoadingInstalledSoftware = false;
                AddSoftwareToDeviceCommand.NotifyCanExecuteChanged();
                RemoveSoftwareFromDeviceCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanExecuteAddSoftwareToDevice() => SelectedDevice != null && !IsLoadingInstalledSoftware;

        private async Task ExecuteAddSoftwareToDeviceCommand()
        {
            if (SelectedDevice == null) return;
            _logger.LogInformation("AddSoftwareToDeviceCommand executed for Device ID: {DeviceId}", SelectedDevice.DeviceId);

            try
            {
                // MODIFIED: Use injected _loggerFactory
                var softwareDialogVm = new WpfApp1.ViewModels.Dialogs.AddSoftwareToDeviceDialogViewModel(_softwareService, _loggerFactory.CreateLogger<WpfApp1.ViewModels.Dialogs.AddSoftwareToDeviceDialogViewModel>());

                // Placeholder for dialog interaction logic as per original code.
                // Ensure your IDialogService is equipped to handle ViewModels like softwareDialogVm.
                // The following commented section is from your original code and illustrates the intended logic.

                await softwareDialogVm.LoadAvailableSoftwareAsync();

                // bool? dialogAccepted = await _dialogService.ShowDialogAsync(softwareDialogVm, "Добавить ПО к устройству");

                // if (dialogAccepted == true && softwareDialogVm.SelectedSoftwareForDialog != null)
                // {
                //     int softwareId = softwareDialogVm.SelectedSoftwareForDialog.SoftwareId;
                //     DateTime installedAt = softwareDialogVm.InstallationDateForDialog;
                //
                //     _logger.LogInformation("Attempting to add Software ID: {SoftwareId} to Device ID: {DeviceId} installed on {InstalledAt}",
                //                            softwareId, SelectedDevice.DeviceId, installedAt);
                //
                //     var addedLink = await _deviceSoftwareService.AddSoftwareToDeviceAsync(SelectedDevice.DeviceId, softwareId, installedAt);
                //
                //     if (addedLink != null)
                //     {
                //         await _dialogService.ShowMessageAsync("Успех", "Программное обеспечение успешно добавлено к устройству.");
                //         await LoadInstalledSoftwareAsync();
                //     }
                //     else
                //     {
                //         await _dialogService.ShowMessageAsync("Информация", "Данное ПО уже установлено на этом устройстве или произошла ошибка.");
                //     }
                // }
                // else
                // {
                //     _logger.LogInformation("Add software dialog cancelled or closed without selection.");
                // }
                await _dialogService.ShowMessageAsync("Информация", "Логика диалога добавления ПО требует завершения реализации с DialogService."); // Placeholder message
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing AddSoftwareToDeviceCommand for Device ID: {DeviceId}", SelectedDevice.DeviceId);
                await _dialogService.ShowErrorAsync("Ошибка", "Произошла ошибка при добавлении ПО к устройству.", ex.ToString());
            }
        }

        private bool CanExecuteRemoveSoftwareFromDevice(DeviceSoftware? deviceSoftware) => (deviceSoftware ?? SelectedInstalledSoftwareItem) != null && !IsLoadingInstalledSoftware;

        private async Task ExecuteRemoveSoftwareFromDeviceCommand(DeviceSoftware? deviceSoftware)
        {
            var softwareToRemove = deviceSoftware ?? SelectedInstalledSoftwareItem;

            if (softwareToRemove == null)
            {
                _logger.LogWarning("RemoveSoftwareFromDeviceCommand executed with null software item.");
                return;
            }

            _logger.LogInformation("RemoveSoftwareFromDeviceCommand executed for DeviceSoftware ID: {DeviceSoftwareId} (Software ID: {SoftwareId} on Device ID: {DeviceId})",
                                     softwareToRemove.Id, softwareToRemove.SoftwareId, softwareToRemove.DeviceId);

            var confirmed = await _dialogService.ShowConfirmAsync(
                "Удаление ПО с устройства",
                $"Вы уверены, что хотите удалить '{softwareToRemove.Software?.Name ?? "ПО"}' с устройства '{SelectedDevice?.Name ?? "устройство"}'?");

            if (confirmed)
            {
                IsLoadingInstalledSoftware = true; // Manage loading state
                RemoveSoftwareFromDeviceCommand.NotifyCanExecuteChanged(); // Update command state
                try
                {
                    bool success = await _deviceSoftwareService.RemoveSoftwareFromDeviceAsync(softwareToRemove.Id);

                    if (success)
                    {
                        _logger.LogInformation("DeviceSoftware ID: {DeviceSoftwareId} removed successfully.", softwareToRemove.Id);
                        await _dialogService.ShowMessageAsync("Успех", "ПО успешно удалено с устройства.");
                        SelectedDeviceInstalledSoftware.Remove(softwareToRemove);
                        if (SelectedInstalledSoftwareItem == softwareToRemove) SelectedInstalledSoftwareItem = null;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to remove DeviceSoftware ID: {DeviceSoftwareId}.", softwareToRemove.Id);
                        await _dialogService.ShowErrorAsync("Ошибка", "Не удалось удалить ПО с устройства.", "Сервис вернул ошибку.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing DeviceSoftware ID: {DeviceSoftwareId}", softwareToRemove.Id);
                    await _dialogService.ShowErrorAsync("Ошибка", "Произошла ошибка при удалении ПО с устройства.", ex.ToString());
                }
                finally
                {
                    IsLoadingInstalledSoftware = false;
                    RemoveSoftwareFromDeviceCommand.NotifyCanExecuteChanged(); // Reset command state
                }
            }
            else
            {
                _logger.LogInformation("Removal of DeviceSoftware ID: {DeviceSoftwareId} cancelled by user.", softwareToRemove.Id);
            }
        }


        private async Task ExecuteAddDeviceCommand()
        {
            _logger.LogInformation("AddDeviceCommand executed.");
            // MODIFIED: Use injected dependencies
            var addEditDeviceViewModel = new AddEditDeviceViewModel(_deviceService, _dialogService, _applicationStateService, _userService, _loggerFactory.CreateLogger<AddEditDeviceViewModel>());

            bool? result = await _dialogService.ShowDialogAsync(addEditDeviceViewModel, "Добавить новое устройство");

            if (result == true)
            {
                _logger.LogInformation("New device saved, reloading devices.");
                await LoadDevicesAsync();
            }
            else
            {
                _logger.LogInformation("Add device dialog cancelled or closed without saving.");
            }
        }

        private async Task ExecuteEditDeviceCommand(Device? device)
        {
            if (device == null) { _logger.LogWarning("EditDeviceCommand called with null device."); return; }
            _logger.LogInformation($"EditDeviceCommand executed for device ID: {device.DeviceId}.");

            var deviceToEdit = await _deviceService.GetDeviceByIdAsync(device.DeviceId);
            if (deviceToEdit == null)
            {
                await _dialogService.ShowErrorAsync("Ошибка", "Устройство не найдено или было удалено.", "");
                return;
            }

            // MODIFIED: Use injected dependencies
            var addEditDeviceViewModel = new AddEditDeviceViewModel(_deviceService, _dialogService, _applicationStateService, _userService, _loggerFactory.CreateLogger<AddEditDeviceViewModel>(), deviceToEdit);

            bool? result = await _dialogService.ShowDialogAsync(addEditDeviceViewModel, $"Редактировать устройство: {deviceToEdit.Name}");

            if (result == true)
            {
                _logger.LogInformation($"Device ID: {deviceToEdit.DeviceId} updated, reloading devices.");
                await LoadDevicesAsync();
            }
            else
            {
                _logger.LogInformation($"Edit device ID: {deviceToEdit.DeviceId} dialog cancelled or closed without saving.");
            }
        }

        private async Task ExecuteDeleteDeviceCommand(Device? device)
        {
            if (device == null) { _logger.LogWarning("DeleteDeviceCommand called with null device."); return; }
            _logger.LogInformation($"DeleteDeviceCommand executed for device ID: {device.DeviceId}.");

            var confirmed = await _dialogService.ShowConfirmAsync("Удаление устройства", $"Вы уверены, что хотите удалить устройство \"{device.Name}\" (Инв. номер: {device.InventoryNumber})?");
            if (confirmed)
            {
                IsBusy = true;
                DeleteDeviceCommand.NotifyCanExecuteChanged(); // Reflect IsBusy change on command
                EditDeviceCommand.NotifyCanExecuteChanged();   // Also affects edit
                LoadDevicesCommand.NotifyCanExecuteChanged(); // And loading
                try
                {
                    await _deviceService.DeleteDeviceAsync(device.DeviceId);
                    _logger.LogInformation($"Device ID: {device.DeviceId} deleted successfully.");
                    await LoadDevicesAsync(); // Refresh the list
                    await _dialogService.ShowMessageAsync("Успех", "Устройство успешно удалено.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deleting device ID: {device.DeviceId}.");
                    await _dialogService.ShowErrorAsync("Ошибка", $"Произошла ошибка при удалении устройства: {ex.Message}", ex.ToString());
                }
                finally
                {
                    IsBusy = false;
                    DeleteDeviceCommand.NotifyCanExecuteChanged();
                    EditDeviceCommand.NotifyCanExecuteChanged();
                    LoadDevicesCommand.NotifyCanExecuteChanged();
                }
            }
            else
            {
                _logger.LogInformation($"Deletion of device ID: {device.DeviceId} cancelled by user.");
            }
        }
        // REMOVED: Placeholder for ILoggerFactory is now injected.
        // private Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory = new Microsoft.Extensions.Logging.LoggerFactory(); 
    }
}