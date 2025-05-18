// File: WpfApp1/ViewModels/Dialogs/AddEditTicketDialogViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WpfApp1.Interfaces; // Убедитесь, что DialogCloseRequestedEventArgs здесь или в Services
using WpfApp1.Models;
using WpfApp1.Services; // Для DialogCloseRequestedEventArgs, если он там

namespace WpfApp1.ViewModels.Dialogs
{
    public partial class AddEditTicketDialogViewModel : ObservableObject, IDialogViewModel
    {
        private readonly ITicketService _ticketService;
        private readonly IUserService _userService;
        private readonly IDeviceService _deviceService;
        private readonly IDialogService _internalDialogService;
        private readonly ILogger<AddEditTicketDialogViewModel> _logger;
        private bool _isEditMode = false;

        // --- Реализация IDialogViewModel ---
        private string _title = "Заявка"; // Заголовок по умолчанию
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value); // Публичный сеттер
        }

        private bool? _dialogResult;
        public bool? DialogResult => _dialogResult; // Только getter, как в интерфейсе

        public event Action<bool?>? CloseRequested; // Корректный тип события

        public void RequestClose(bool? result) // Реализация метода
        {
            _dialogResult = result;
            CloseRequested?.Invoke(result);
        }
        // --- Конец реализации IDialogViewModel ---

        [ObservableProperty]
        private Ticket _currentTicket = new Ticket();

        [ObservableProperty]
        private bool _isLoading = false;

        // CanSave удалено, так как RelayCommand использует CanExecuteSave
        // [ObservableProperty]
        // private bool _canSave = false;

        public ObservableCollection<TicketStatus> AvailableStatuses { get; } = new ObservableCollection<TicketStatus>();
        public ObservableCollection<TicketPriority> AvailablePriorities { get; } = new ObservableCollection<TicketPriority>();
        public ObservableCollection<User> AvailableUsers { get; } = new ObservableCollection<User>();
        public ObservableCollection<Device> AvailableDevices { get; } = new ObservableCollection<Device>();

        [ObservableProperty]
        private TicketStatus? _selectedStatus;
        [ObservableProperty]
        private TicketPriority? _selectedPriority;
        [ObservableProperty]
        private User? _selectedAssignee;
        [ObservableProperty]
        private Device? _selectedDevice;

        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public AddEditTicketDialogViewModel(
            ITicketService ticketService,
            IUserService userService,
            IDeviceService deviceService,
            IDialogService dialogService,
            ILogger<AddEditTicketDialogViewModel> logger)
        {
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
            _internalDialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // Убрал PropertyChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
            // Вместо этого используем атрибуты [NotifyCanExecuteChangedFor(nameof(SaveCommand))] для свойств,
            // от которых зависит CanExecuteSave, или обновляем вручную где необходимо.
            // Для CurrentTicket.Title используем [AlsoNotifyChangeFor(nameof(CanSave))] если CurrentTicket - ObservableObject
            // или подписываемся на его PropertyChanged.
        }

        public async Task InitializeAsync(Ticket ticket)
        {
            if (ticket == null) throw new ArgumentNullException(nameof(ticket));
            IsLoading = true;
            // CanSave = false; // Кнопка будет управляться через CanExecuteSave
            SaveCommand.NotifyCanExecuteChanged(); // Обновить состояние кнопки
            _logger.LogInformation("Initializing AddEditTicketDialogViewModel for TicketId: {TicketId}", ticket.TicketId);

            _isEditMode = ticket.TicketId > 0;
            Title = _isEditMode ? $"Заявка (ID: {ticket.TicketId}) - Редактирование" : "Заявка - Создание";
            // OnPropertyChanged(nameof(Title)); // SetProperty уже это сделает

            CurrentTicket = CloneTicket(ticket); // Предполагается, что CurrentTicket вызовет PropertyChanged

            try
            {
                var statusesTask = _ticketService.GetAvailableStatusesAsync();
                var prioritiesTask = _ticketService.GetAvailablePrioritiesAsync();
                // Изменяем вызов GetAllActiveUsersAsync на GetAllUsersAsync с последующей фильтрацией
                var usersTask = _userService.GetAllUsersAsync();
                var devicesTask = _deviceService.GetDevicesAsync(filters: null, sortBy: "Name", ascending: true);

                await Task.WhenAll(statusesTask, prioritiesTask, usersTask, devicesTask);

                AvailableStatuses.Clear();
                foreach (var status in await statusesTask) AvailableStatuses.Add(status);

                AvailablePriorities.Clear();
                foreach (var priority in await prioritiesTask) AvailablePriorities.Add(priority);

                AvailableUsers.Clear();
                // Фильтруем пользователей по IsActive
                foreach (var user in (await usersTask).Where(u => u.IsActive).OrderBy(u => u.FullName))
                {
                    AvailableUsers.Add(user);
                }

                AvailableDevices.Clear();
                foreach (var device in await devicesTask) AvailableDevices.Add(device);

                SelectedStatus = AvailableStatuses.FirstOrDefault(s => s.StatusId == CurrentTicket.StatusId);
                SelectedPriority = AvailablePriorities.FirstOrDefault(p => p.PriorityId == CurrentTicket.PriorityId);
                SelectedAssignee = AvailableUsers.FirstOrDefault(u => u.UserId == CurrentTicket.AssigneeId); // Добавлено для согласованности
                SelectedDevice = AvailableDevices.FirstOrDefault(d => d.DeviceId == CurrentTicket.DeviceId); // Используем DeviceId, как и в модели Ticket

                if (!_isEditMode)
                {
                    SelectedStatus ??= AvailableStatuses.FirstOrDefault();
                    SelectedPriority ??= AvailablePriorities.FirstOrDefault();
                }
                _logger.LogInformation("Initialization complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing AddEditTicketDialogViewModel.");
                await _internalDialogService.ShowMessageAsync("Ошибка", "Не удалось загрузить данные для диалога.");
                RequestClose(false); // Используем метод интерфейса
            }
            finally
            {
                IsLoading = false;
                SaveCommand.NotifyCanExecuteChanged(); // Обновить состояние кнопки после загрузки
            }
        }

        private Ticket CloneTicket(Ticket source)
        {
            return new Ticket
            {
                TicketId = source.TicketId,
                Title = source.Title,
                Description = source.Description,
                CreatedAt = source.CreatedAt,
                RequesterId = source.RequesterId, // Используем RequesterId
                AssigneeId = source.AssigneeId,   // Используем AssigneeId
                StatusId = source.StatusId,
                PriorityId = source.PriorityId,
                DeviceId = source.DeviceId,
                Deadline = source.Deadline,
                Category = source.Category, // Не забываем категорию
                                            // ClosedAt не используется в "Правках" для клонирования, оставим так
            };
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(CurrentTicket.Title) &&
                   SelectedStatus != null &&
                   SelectedPriority != null &&
                   !IsLoading;
        }

        private async Task ExecuteSaveAsync()
        {
            if (!CanExecuteSave()) return;

            _logger.LogInformation("SaveCommand executed for TicketId: {TicketId}", CurrentTicket.TicketId);
            IsLoading = true;
            SaveCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();

            try
            {
                CurrentTicket.StatusId = SelectedStatus!.StatusId; // Not null из-за CanExecuteSave
                CurrentTicket.PriorityId = SelectedPriority!.PriorityId; // Not null
                CurrentTicket.AssigneeId = SelectedAssignee?.UserId; // Может быть null
                CurrentTicket.DeviceId = SelectedDevice?.DeviceId;   // Может быть null

                bool success = false;
                if (_isEditMode)
                {
                    var updatedTicket = await _ticketService.UpdateTicketAsync(CurrentTicket);
                    success = updatedTicket != null;
                }
                else
                {
                    // Установка RequesterId для новой заявки (если это не делается автоматически)
                    // CurrentTicket.RequesterId = _applicationStateService.CurrentUser.UserId; // Пример
                    var addedTicket = await _ticketService.AddTicketAsync(CurrentTicket);
                    success = addedTicket != null && addedTicket.TicketId > 0;
                }

                if (success)
                {
                    _logger.LogInformation("Ticket saved successfully.");
                    RequestClose(true); // Используем метод интерфейса
                }
                else
                {
                    _logger.LogWarning("Failed to save ticket.");
                    await _internalDialogService.ShowMessageAsync("Ошибка", "Не удалось сохранить заявку.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving ticket.");
                await _internalDialogService.ShowMessageAsync("Ошибка", $"Произошла ошибка при сохранении: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                SaveCommand.NotifyCanExecuteChanged();
                CancelCommand.NotifyCanExecuteChanged();
            }
        }

        private void ExecuteCancel()
        {
            _logger.LogInformation("CancelCommand executed.");
            RequestClose(false); // Используем метод интерфейса
        }

        // Обновление CanExecute команд при изменении свойств
        partial void OnCurrentTicketChanged(Ticket? oldValue, Ticket newValue) => SaveCommand.NotifyCanExecuteChanged();
        partial void OnSelectedStatusChanged(TicketStatus? value) => SaveCommand.NotifyCanExecuteChanged();
        partial void OnSelectedPriorityChanged(TicketPriority? value) => SaveCommand.NotifyCanExecuteChanged();
        partial void OnIsLoadingChanged(bool value)
        {
            SaveCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
        }
    }
}