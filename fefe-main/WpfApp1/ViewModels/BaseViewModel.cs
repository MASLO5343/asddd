// WpfApp1/ViewModels/BaseViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks; // Added for Task

namespace WpfApp1.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        // Используем [ObservableProperty] для часто используемых свойств, если они есть,
        // например, для заголовка или состояния занятости.

        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        public bool IsNotBusy => !IsBusy;

        // Вы можете добавить сюда любую общую логику или свойства для ваших ViewModel.
        // ObservableObject уже предоставляет реализацию INotifyPropertyChanged.

        /// <summary>
        /// Called when the ViewModel is navigated to.
        /// </summary>
        /// <param name="parameter">The navigation parameter.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual Task OnNavigatedTo(object parameter)
        {
            // Default implementation, can be overridden in derived classes
            return Task.CompletedTask;
        }
    }
}