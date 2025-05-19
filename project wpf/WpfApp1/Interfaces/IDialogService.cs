using System.Threading.Tasks;
using WpfApp1.Models; // For specific dialogs if any remain, though preference is generic
using System.Collections.Generic; // For IEnumerable if needed by specific dialogs

namespace WpfApp1.Interfaces
{
    public interface IDialogService
    {
        void ShowMessage(string title, string message);
        Task ShowMessageAsync(string title, string message);

        void ShowError(string title, string message, string details = "");
        Task ShowErrorAsync(string title, string message, string details = "");

        bool ShowConfirm(string title, string message);
        Task<bool> ShowConfirmAsync(string title, string message);
        void ShowWarning(string title, string message);
        Task ShowWarningAsync(string title, string message); // Асинхронная версия, если нужна
        /// <summary>
        /// Shows a dialog with the given ViewModel and returns a boolean result.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the ViewModel for the dialog content.</typeparam>
        /// <param name="viewModel">The instance of the ViewModel.</param>
        /// <param name="title">The title of the dialog window.</param>
        /// <returns>A boolean? indicating how the dialog was closed (true for OK, false for Cancel, null otherwise).</returns>
        Task<bool?> ShowDialogAsync<TViewModel>(TViewModel viewModel, string title)
            where TViewModel : IDialogViewModel;

        /// <summary>
        /// Shows a dialog with the given ViewModel and returns a custom result.
        /// </summary>
        /// <typeparam name="TViewModel">The type of the ViewModel for the dialog content.</typeparam>
        /// <typeparam name="TDialogResult">The type of the result expected from the dialog.</typeparam>
        /// <param name="viewModel">The instance of the ViewModel.</param>
        /// <param name="title">The title of the dialog window.</param>
        /// <returns>The custom result from the dialog, or default if closed without a result.</returns>
        Task<TDialogResult> ShowCustomDialogAsync<TViewModel, TDialogResult>(TViewModel viewModel, string title)
            where TViewModel : IDialogViewModel<TDialogResult>;

        // Keep specific methods if they have very unique setup/result logic not covered by generics
        // Example: Task<User> ShowAddEditUserDialogAsync(User userToEdit, IEnumerable<Role> roles);
        // However, aim to convert these to use the generic versions with appropriate ViewModels.
    }
}