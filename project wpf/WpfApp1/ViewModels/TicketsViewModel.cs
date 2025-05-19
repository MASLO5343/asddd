// File: WpfApp1/ViewModels/TicketsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection; // <-- Добавлено для IServiceProvider
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using WpfApp1.Interfaces;
using WpfApp1.Models;
using WpfApp1.Services.Data;
using System.Collections.Generic;
using WpfApp1.ViewModels.Dialogs; // <-- Добавлено для AddEditTicketDialogViewModel

namespace WpfApp1.ViewModels
{
    public partial class TicketsViewModel : BaseViewModel
    {
        private readonly ITicketService _ticketService;
        private readonly ITicketCommentService _ticketCommentService;
        private readonly IApplicationStateService _applicationStateService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<TicketsViewModel> _logger;
        private readonly IUserService _userService;
        private readonly IDeviceService _deviceService;
        private readonly IServiceProvider _serviceProvider; // <-- Сохраняем

        // ... (Existing properties remain the same) ...
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isLoadingFilters;
        [ObservableProperty] private bool _isLoadingComments;
        [ObservableProperty][NotifyCanExecuteChangedFor(nameof(EditTicketCommand))][NotifyCanExecuteChangedFor(nameof(DeleteTicketCommand))][NotifyCanExecuteChangedFor(nameof(AddCommentCommand))] private Ticket? _selectedTicket;
        public ObservableCollection<Ticket> Tickets { get; } = new ObservableCollection<Ticket>();
        public ObservableCollection<TicketComment> SelectedTicketComments { get; } = new ObservableCollection<TicketComment>();
        [ObservableProperty][NotifyCanExecuteChangedFor(nameof(AddCommentCommand))] private string? _newCommentText;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsFiltered))] private string? _searchTextFilter;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsFiltered))] private TicketStatus? _selectedStatusFilter;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsFiltered))] private TicketPriority? _selectedPriorityFilter;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsFiltered))] private User? _selectedAssigneeFilter;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsFiltered))] private User? _selectedCreatorFilter;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsFiltered))] private Device? _selectedDeviceFilter;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsFiltered))] private DateTime? _filterDateFrom;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsFiltered))] private DateTime? _filterDateTo;
        public ObservableCollection<TicketStatus> AvailableStatuses { get; } = new ObservableCollection<TicketStatus>();
        public ObservableCollection<TicketPriority> AvailablePriorities { get; } = new ObservableCollection<TicketPriority>();
        public ObservableCollection<User> AvailableAssignees { get; } = new ObservableCollection<User>();
        public ObservableCollection<User> AvailableCreators { get; } = new ObservableCollection<User>();
        public ObservableCollection<Device> AvailableDevices { get; } = new ObservableCollection<Device>();
        [ObservableProperty] private string _sortColumn = "CreatedAt";
        [ObservableProperty] private bool _isSortAscending = false;
        public bool IsFiltered => !string.IsNullOrWhiteSpace(SearchTextFilter) || SelectedStatusFilter != null || /* ... other filters ... */ FilterDateTo != null;


        // --- Команды ---
        public IAsyncRelayCommand CreateTicketCommand { get; }
        public IAsyncRelayCommand EditTicketCommand { get; }
        public IAsyncRelayCommand DeleteTicketCommand { get; }
        public IAsyncRelayCommand RefreshTicketsCommand { get; }
        public IRelayCommand<string> SortCommand { get; }
        public IRelayCommand ClearFiltersCommand { get; }
        public IAsyncRelayCommand AddCommentCommand { get; }
        public IAsyncRelayCommand<TicketComment> DeleteCommentCommand { get; }


        // Конструктор с IServiceProvider
        public TicketsViewModel(
            ITicketService ticketService, ITicketCommentService ticketCommentService,
            IApplicationStateService applicationStateService, IDialogService dialogService,
            ILogger<TicketsViewModel> logger, IUserService userService,
            IDeviceService deviceService, IServiceProvider serviceProvider)
        {
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
            _ticketCommentService = ticketCommentService ?? throw new ArgumentNullException(nameof(ticketCommentService));
            _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
            _serviceProvider = serviceProvider; // <-- Сохраняем

            Title = "Управление заявками"; // Or from resources

            CreateTicketCommand = new AsyncRelayCommand(ExecuteCreateTicketAsync);
            EditTicketCommand = new AsyncRelayCommand(ExecuteEditTicketAsync, CanExecuteEditOrDeleteTicket);
            DeleteTicketCommand = new AsyncRelayCommand(ExecuteDeleteTicketAsync, CanExecuteEditOrDeleteTicket);
            RefreshTicketsCommand = new AsyncRelayCommand(LoadInitialDataAsync); // Assumes LoadInitialDataAsync is defined
            SortCommand = new RelayCommand<string>(ExecuteSortCommand);          // Assumes ExecuteSortCommand is defined
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);        // Assumes ExecuteClearFilters is defined
            AddCommentCommand = new AsyncRelayCommand(ExecuteAddCommentAsync, CanExecuteAddComment); // Assumes ExecuteAddCommentAsync and CanExecuteAddComment are defined
            DeleteCommentCommand = new AsyncRelayCommand<TicketComment>(ExecuteDeleteCommentAsync, CanExecuteDeleteComment); // Assumes ExecuteDeleteCommentAsync and CanExecuteDeleteComment are defined

            // Listen for changes on SelectedTicket to update command states
            PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(SelectedTicket))
                {
                    EditTicketCommand.NotifyCanExecuteChanged();
                    DeleteTicketCommand.NotifyCanExecuteChanged();
                    AddCommentCommand.NotifyCanExecuteChanged(); // AddComment also depends on SelectedTicket
                }
                if (e.PropertyName == nameof(NewCommentText))
                {
                    AddCommentCommand.NotifyCanExecuteChanged();
                }
            };
        }

        // This method is called when the ViewModel is navigated to.
        public override async Task OnNavigatedTo(object parameter)
        {
            await LoadInitialDataAsync();
            await base.OnNavigatedTo(parameter);
        }

        private async Task LoadInitialDataAsync()
        {
            if (IsLoading || IsLoadingFilters) return;

            IsLoading = true;
            IsLoadingFilters = true;
            _logger.LogInformation("TicketsViewModel: Loading initial data (filters and tickets)...");
            try
            {
                var loadFiltersTask = LoadFilterCollectionsAsync();
                var loadTicketsTask = LoadTicketsAsync(false); // Load all tickets initially

                await Task.WhenAll(loadFiltersTask, loadTicketsTask);

                SelectedTicketComments.Clear(); // Clear comments if any were previously loaded
                NewCommentText = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TicketsViewModel: Error loading initial data.");
                await _dialogService.ShowErrorAsync("Ошибка загрузки", "Не удалось загрузить начальные данные для заявок.", ex.ToString());
            }
            finally
            {
                IsLoading = false;
                IsLoadingFilters = false;
                _logger.LogInformation("TicketsViewModel: Initial data loading complete.");
            }
        }

        private async Task LoadFilterCollectionsAsync()
        {
            IsLoadingFilters = true;
            _logger.LogInformation("Loading filter collections for tickets...");
            try
            {
                var statusesTask = _ticketService.GetAvailableStatusesAsync();
                var prioritiesTask = _ticketService.GetAvailablePrioritiesAsync();
                var usersTask = _userService.GetAllUsersAsync(); // For Assignees and Creators
                var devicesTask = _deviceService.GetDevicesAsync(null, "Name", true, 1, 1000); // Get a reasonable number of devices for filtering

                await Task.WhenAll(statusesTask, prioritiesTask, usersTask, devicesTask);

                AvailableStatuses.Clear();
                foreach (var status in (await statusesTask).OrderBy(s => s.SortOrder)) AvailableStatuses.Add(status);

                AvailablePriorities.Clear();
                foreach (var priority in (await prioritiesTask).OrderBy(p => p.SortOrder)) AvailablePriorities.Add(priority);

                var userList = (await usersTask).Where(u => u.IsActive).OrderBy(u => u.FullName).ToList();
                AvailableAssignees.Clear();
                AvailableCreators.Clear();
                foreach (var user in userList)
                {
                    AvailableAssignees.Add(user);
                    AvailableCreators.Add(user);
                }

                AvailableDevices.Clear();
                foreach (var device in await devicesTask) AvailableDevices.Add(device);

                _logger.LogInformation("Ticket filter collections loaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ticket filter collections.");
                await _dialogService.ShowErrorAsync("Ошибка фильтров", "Не удалось загрузить данные для фильтров заявок.", ex.ToString());
            }
            finally
            {
                IsLoadingFilters = false;
            }
        }

        private async Task LoadTicketsAsync(bool applyFilters = true)
        {
            if (IsLoading) return;
            IsLoading = true;
            _logger.LogInformation("Loading tickets... ApplyFilters: {ApplyFilters}", applyFilters);

            try
            {
                TicketFilterParameters? currentFilters = null;
                if (applyFilters)
                {
                    currentFilters = new TicketFilterParameters
                    {
                        SearchText = SearchTextFilter,
                        StatusId = SelectedStatusFilter?.StatusId,
                        PriorityId = SelectedPriorityFilter?.PriorityId,
                        AssignedToUserId = SelectedAssigneeFilter?.UserId,
                        CreatedByUserId = SelectedCreatorFilter?.UserId,
                        DeviceId = SelectedDeviceFilter?.DeviceId,
                        DateFrom = FilterDateFrom,
                        DateTo = FilterDateTo
                    };
                }

                var ticketsList = await _ticketService.GetAllTicketsAsync(currentFilters, SortColumn, IsSortAscending);
                Tickets.Clear();
                foreach (var ticket in ticketsList)
                {
                    Tickets.Add(ticket);
                }
                _logger.LogInformation($"Loaded {Tickets.Count} tickets.");
                if (Tickets.Any() && SelectedTicket == null)
                {
                    // SelectedTicket = Tickets.First(); // Optionally select the first ticket
                }
                else if (!Tickets.Any())
                {
                    SelectedTicket = null; // Clear selection if no tickets
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tickets.");
                await _dialogService.ShowErrorAsync("Ошибка загрузки заявок", "Не удалось загрузить список заявок.", ex.ToString());
            }
            finally
            {
                IsLoading = false;
            }
        }


        partial void OnSearchTextFilterChanged(string? oldValue, string? newValue) => _ = LoadTicketsAsync();
        partial void OnSelectedStatusFilterChanged(TicketStatus? oldValue, TicketStatus? newValue) => _ = LoadTicketsAsync();
        partial void OnSelectedPriorityFilterChanged(TicketPriority? oldValue, TicketPriority? newValue) => _ = LoadTicketsAsync();
        partial void OnSelectedAssigneeFilterChanged(User? oldValue, User? newValue) => _ = LoadTicketsAsync();
        partial void OnSelectedCreatorFilterChanged(User? oldValue, User? newValue) => _ = LoadTicketsAsync();
        partial void OnSelectedDeviceFilterChanged(Device? oldValue, Device? newValue) => _ = LoadTicketsAsync();
        partial void OnFilterDateFromChanged(DateTime? oldValue, DateTime? newValue) => _ = LoadTicketsAsync();
        partial void OnFilterDateToChanged(DateTime? oldValue, DateTime? newValue) => _ = LoadTicketsAsync();


        private void ExecuteSortCommand(string? columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return;

            _logger.LogInformation($"Sorting tickets by column: {columnName}. Current sort: {SortColumn} {(IsSortAscending ? "ASC" : "DESC")}");
            if (SortColumn == columnName)
            {
                IsSortAscending = !IsSortAscending;
            }
            else
            {
                SortColumn = columnName;
                IsSortAscending = true; // Default to ascending for a new column
            }
            _logger.LogInformation($"New sort: {SortColumn} {(IsSortAscending ? "ASC" : "DESC")}");
            _ = LoadTicketsAsync();
        }

        private void ExecuteClearFilters()
        {
            _logger.LogInformation("Clearing all ticket filters.");
            SearchTextFilter = null;
            SelectedStatusFilter = null;
            SelectedPriorityFilter = null;
            SelectedAssigneeFilter = null;
            SelectedCreatorFilter = null;
            SelectedDeviceFilter = null;
            FilterDateFrom = null;
            FilterDateTo = null;
            // LoadTicketsAsync() will be triggered by individual property changes if they are observable and call it.
            // If not, uncomment the line below. For safety, it's good to ensure it's called once after all are set.
            _ = LoadTicketsAsync(false); // Explicitly reload all tickets without filters
        }

        // --- Обновленная Реализация CRUD Команд ---
        private async Task ExecuteCreateTicketAsync()
        {
            _logger.LogInformation("CreateTicketCommand executed.");
            try
            {
                var dialogViewModel = _serviceProvider.GetRequiredService<AddEditTicketDialogViewModel>();
                await dialogViewModel.InitializeAsync(new Ticket() { RequesterId = _applicationStateService.CurrentUser?.UserId ?? 0 }); // Pass new ticket, ensure RequesterId is set

                // FIX CS1061: Call ShowDialogAsync with a title
                bool? result = await _dialogService.ShowDialogAsync(dialogViewModel, dialogViewModel.Title ?? "Создание новой заявки");

                if (result == true)
                {
                    _logger.LogInformation("New ticket created successfully via dialog. Reloading tickets.");
                    await LoadTicketsAsync(false);
                }
                else
                {
                    _logger.LogInformation("Create ticket dialog cancelled.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during create ticket process.");
                // CS0103: Assuming LoadTicketsAsync is correctly defined for the call below (it is).
                await LoadTicketsAsync(false); // Error on this line 111 in user's paste.
                await _dialogService.ShowMessageAsync("Ошибка", $"Не удалось открыть диалог создания заявки: {ex.Message}");
            }
        }

        private bool CanExecuteEditOrDeleteTicket() => SelectedTicket != null && !IsLoading;

        private async Task ExecuteEditTicketAsync()
        {
            if (!CanExecuteEditOrDeleteTicket() || SelectedTicket == null) return;
            _logger.LogInformation("EditTicketCommand executed for Ticket ID: {TicketId}", SelectedTicket.TicketId);

            try
            {
                var ticketToEdit = await _ticketService.GetTicketByIdAsync(SelectedTicket.TicketId);
                if (ticketToEdit == null)
                {
                    _logger.LogWarning("Ticket ID: {TicketId} not found for editing.", SelectedTicket.TicketId);
                    await _dialogService.ShowMessageAsync("Ошибка", "Не удалось загрузить данные заявки для редактирования. Возможно, она была удалена.");
                    // CS0103: Assuming LoadTicketsAsync is correctly defined (it is)
                    await LoadTicketsAsync(false); // Line 154 in user's paste
                    return;
                }

                var dialogViewModel = _serviceProvider.GetRequiredService<AddEditTicketDialogViewModel>();
                await dialogViewModel.InitializeAsync(ticketToEdit);

                // FIX CS1061: Call ShowDialogAsync with a title
                bool? result = await _dialogService.ShowDialogAsync(dialogViewModel, dialogViewModel.Title ?? $"Редактирование заявки: {ticketToEdit.Title}");

                if (result == true)
                {
                    _logger.LogInformation("Ticket ID: {TicketId} edited successfully via dialog. Reloading tickets.", SelectedTicket.TicketId);
                    await LoadTicketsAsync(false);
                }
                else
                {
                    _logger.LogInformation("Edit ticket dialog cancelled for Ticket ID: {TicketId}.", SelectedTicket.TicketId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during edit ticket process for Ticket ID: {TicketId}.", SelectedTicket?.TicketId);
                await _dialogService.ShowMessageAsync("Ошибка", $"Не удалось открыть диалог редактирования заявки: {ex.Message}");
            }
        }

        private async Task ExecuteDeleteTicketAsync()
        {
            if (!CanExecuteEditOrDeleteTicket() || SelectedTicket == null) return;
            _logger.LogInformation("DeleteTicketCommand executed for Ticket ID: {TicketId}", SelectedTicket.TicketId);

            // FIX CS1061: Change ShowConfirmationAsync to ShowConfirmAsync
            var confirmed = await _dialogService.ShowConfirmAsync("Удаление заявки", $"Вы уверены, что хотите удалить заявку '{SelectedTicket.Title}' (ID: {SelectedTicket.TicketId})?");

            if (confirmed)
            {
                IsLoading = true;
                EditTicketCommand.NotifyCanExecuteChanged(); // Update related commands
                DeleteTicketCommand.NotifyCanExecuteChanged();
                try
                {
                    bool success = await _ticketService.DeleteTicketAsync(SelectedTicket.TicketId);
                    if (success)
                    {
                        _logger.LogInformation("Ticket ID: {TicketId} deleted successfully.", SelectedTicket.TicketId);
                        await _dialogService.ShowMessageAsync("Успех", "Заявка успешно удалена.");
                        // CS0103: Assuming LoadTicketsAsync is correctly defined (it is)
                        await LoadTicketsAsync(false); // Line 178 in user's paste. This will also clear SelectedTicket if it was the one deleted.
                    }
                    else
                    {
                        _logger.LogWarning("Failed to delete Ticket ID: {TicketId}. Service returned false.", SelectedTicket.TicketId);
                        await _dialogService.ShowMessageAsync("Ошибка", "Не удалось удалить заявку. Возможно, она уже была удалена или произошла ошибка на сервере.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting Ticket ID: {TicketId}", SelectedTicket.TicketId);
                    await _dialogService.ShowMessageAsync("Ошибка", $"Не удалось удалить заявку: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                    EditTicketCommand.NotifyCanExecuteChanged();
                    DeleteTicketCommand.NotifyCanExecuteChanged();
                }
            }
            else
            {
                _logger.LogInformation("Deletion of Ticket ID: {TicketId} cancelled by user.", SelectedTicket.TicketId);
            }
        }

        partial void OnSelectedTicketChanged(Ticket? oldValue, Ticket? newValue)
        {
            _logger.LogDebug("SelectedTicket changed from {OldTicketId} to {NewTicketId}", oldValue?.TicketId, newValue?.TicketId);
            EditTicketCommand.NotifyCanExecuteChanged();
            DeleteTicketCommand.NotifyCanExecuteChanged();
            AddCommentCommand.NotifyCanExecuteChanged(); // Also depends on SelectedTicket
            if (newValue != null)
            {
                _ = LoadCommentsAsync(newValue.TicketId);
            }
            else
            {
                SelectedTicketComments.Clear();
                NewCommentText = string.Empty;
            }
        }

        private async Task LoadCommentsAsync(int ticketId)
        {
            if (ticketId <= 0)
            {
                SelectedTicketComments.Clear();
                return;
            }
            IsLoadingComments = true;
            _logger.LogInformation("Loading comments for Ticket ID: {TicketId}", ticketId);
            SelectedTicketComments.Clear();
            try
            {
                // Assuming ITicketCommentService has a method like GetCommentsForTicketAsync
                // This was added in a previous step to TicketCommentService.cs
                var comments = await _ticketCommentService.GetCommentsForTicketAsync(ticketId);
                foreach (var comment in comments)
                {
                    SelectedTicketComments.Add(comment);
                }
                _logger.LogInformation("Loaded {CommentCount} comments for Ticket ID: {TicketId}", SelectedTicketComments.Count, ticketId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading comments for Ticket ID: {TicketId}", ticketId);
                await _dialogService.ShowErrorAsync("Ошибка комментариев", "Не удалось загрузить комментарии к заявке.", ex.ToString());
            }
            finally
            {
                IsLoadingComments = false;
            }
        }

        private bool CanExecuteAddComment()
        {
            return SelectedTicket != null && !string.IsNullOrWhiteSpace(NewCommentText) && !IsLoadingComments;
        }

        private async Task ExecuteAddCommentAsync()
        {
            if (!CanExecuteAddComment() || SelectedTicket == null || string.IsNullOrWhiteSpace(NewCommentText)) return;

            _logger.LogInformation("Attempting to add comment to Ticket ID: {TicketId}", SelectedTicket.TicketId);
            IsLoadingComments = true; // Indicate activity
            try
            {
                var newComment = new TicketComment
                {
                    TicketId = SelectedTicket.TicketId,
                    Comment = NewCommentText, // Property in model is "Comment"
                    // AuthorId will be set by the service based on current user
                    // CreatedAt will be set by the service
                };

                var addedComment = await _ticketCommentService.AddCommentAsync(newComment);
                if (addedComment != null)
                {
                    // SelectedTicketComments.Add(addedComment); // Add to list immediately
                    NewCommentText = string.Empty; // Clear input
                    _logger.LogInformation("Comment added successfully to Ticket ID: {TicketId}", SelectedTicket.TicketId);
                    await LoadCommentsAsync(SelectedTicket.TicketId); // Reload comments to get the fresh list with author info
                }
                else
                {
                    _logger.LogWarning("Failed to add comment to Ticket ID: {TicketId}. Service returned null.", SelectedTicket.TicketId);
                    await _dialogService.ShowMessageAsync("Ошибка", "Не удалось добавить комментарий.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment to Ticket ID: {TicketId}", SelectedTicket.TicketId);
                await _dialogService.ShowErrorAsync("Ошибка комментария", "Произошла ошибка при добавлении комментария.", ex.ToString());
            }
            finally
            {
                IsLoadingComments = false;
            }
        }

        private bool CanExecuteDeleteComment(TicketComment? comment)
        {
            // Only author or admin can delete (example logic, needs proper implementation with roles/permissions)
            if (comment == null || _applicationStateService.CurrentUser == null) return false;
            // For now, allow any logged-in user to delete any comment for testing, or implement proper logic
            // return comment.AuthorId == _applicationStateService.CurrentUser.UserId || _applicationStateService.CurrentUser.Role?.RoleName == "Admin";
            return !IsLoadingComments && SelectedTicket != null; // Basic check
        }

        private async Task ExecuteDeleteCommentAsync(TicketComment? comment)
        {
            if (comment == null || SelectedTicket == null)
            {
                _logger.LogWarning("Attempted to delete a null comment or when no ticket is selected.");
                return;
            }

            _logger.LogInformation("Attempting to delete comment ID: {CommentId} from Ticket ID: {TicketId}", comment.TicketCommentId, SelectedTicket.TicketId);

            var confirmed = await _dialogService.ShowConfirmAsync("Удаление комментария", "Вы уверены, что хотите удалить этот комментарий?");
            if (!confirmed)
            {
                _logger.LogInformation("Deletion of comment ID: {CommentId} cancelled by user.", comment.TicketCommentId);
                return;
            }

            IsLoadingComments = true;
            try
            {
                bool success = await _ticketCommentService.DeleteCommentAsync(comment.TicketCommentId);
                if (success)
                {
                    // SelectedTicketComments.Remove(comment); // Remove from list immediately
                    _logger.LogInformation("Comment ID: {CommentId} deleted successfully from Ticket ID: {TicketId}", comment.TicketCommentId, SelectedTicket.TicketId);
                    await LoadCommentsAsync(SelectedTicket.TicketId); // Reload comments
                }
                else
                {
                    _logger.LogWarning("Failed to delete comment ID: {CommentId}. Service returned false.", comment.TicketCommentId);
                    await _dialogService.ShowMessageAsync("Ошибка", "Не удалось удалить комментарий.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment ID: {CommentId}", comment.TicketCommentId);
                await _dialogService.ShowErrorAsync("Ошибка удаления", "Произошла ошибка при удалении комментария.", ex.ToString());
            }
            finally
            {
                IsLoadingComments = false;
            }
        }
    }
}