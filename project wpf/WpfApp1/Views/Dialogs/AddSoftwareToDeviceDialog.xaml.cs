// File: WpfApp1/ViewModels/Dialogs/AddSoftwareToDeviceDialogViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel; // Для ObservableObject
using CommunityToolkit.Mvvm.Input;         // Для RelayCommand
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows; // Для Window
using WpfApp1.Interfaces;
using WpfApp1.Models;

namespace WpfApp1.ViewModels.Dialogs
{
    public partial class AddSoftwareToDeviceDialogViewModel : ObservableObject // Используем ObservableObject из CommunityToolkit.Mvvm
    {
        private readonly ISoftwareService _softwareService;
        private readonly ILogger<AddSoftwareToDeviceDialogViewModel> _logger;

        public ObservableCollection<Software> AvailableSoftware { get; } = new ObservableCollection<Software>();

        // Используем атрибуты CommunityToolkit.Mvvm для генерации свойств
        [ObservableProperty]
        private Software? _selectedSoftwareForDialog;

        [ObservableProperty]
        private DateTime _installationDateForDialog = DateTime.Today;

        [ObservableProperty]
        private bool _isLoading;

        // Команды для кнопок ОК и Отмена
        public IRelayCommand<Window> OkCommand { get; }
        public IRelayCommand<Window> CancelCommand { get; }


        public AddSoftwareToDeviceDialogViewModel(ISoftwareService softwareService, ILogger<AddSoftwareToDeviceDialogViewModel> logger)
        {
            _softwareService = softwareService ?? throw new ArgumentNullException(nameof(softwareService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            OkCommand = new RelayCommand<Window>(ExecuteOk, CanExecuteOk);
            CancelCommand = new RelayCommand<Window>(ExecuteCancel);
        }

        // Метод для асинхронной загрузки данных, вызывается из View (Loaded event)
        public async Task LoadAvailableSoftwareAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            _logger.LogInformation("Loading available software for dialog...");
            AvailableSoftware.Clear();
            try
            {
                var softwareList = await _softwareService.GetAllSoftwareAsync();
                foreach (var software in softwareList.OrderBy(s => s.Name))
                {
                    AvailableSoftware.Add(software);
                }
                _logger.LogInformation("Loaded {Count} software items for dialog.", AvailableSoftware.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available software for dialog.");
                // Можно показать сообщение об ошибке, но это диалог, может быть излишне
            }
            finally
            {
                IsLoading = false;
                // Переоценка CanExecute после загрузки данных
                OkCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanExecuteOk(Window? window)
        {
            // Можно нажать ОК только если ПО выбрано и дата корректна (хотя DatePicker обычно сам валидирует)
            return SelectedSoftwareForDialog != null && !IsLoading;
        }

        private void ExecuteOk(Window? window)
        {
            if (window != null && CanExecuteOk(window))
            {
                _logger.LogInformation("OK button clicked. Selected Software ID: {SoftwareId}, Installation Date: {InstallationDate}",
                                      SelectedSoftwareForDialog?.SoftwareId, InstallationDateForDialog);
                window.DialogResult = true; // Устанавливаем результат диалога
                window.Close();
            }
        }

        private void ExecuteCancel(Window? window)
        {
            _logger.LogInformation("Cancel button clicked.");
            if (window != null)
            {
                window.DialogResult = false; // Устанавливаем результат диалога
                window.Close();
            }
        }

        // Обработчик изменения свойства для переоценки CanExecute (генерируется CommunityToolkit.Mvvm)
        partial void OnSelectedSoftwareForDialogChanged(Software? value)
        {
            OkCommand.NotifyCanExecuteChanged();
        }
        partial void OnIsLoadingChanged(bool value)
        {
            OkCommand.NotifyCanExecuteChanged();
        }
    }
}