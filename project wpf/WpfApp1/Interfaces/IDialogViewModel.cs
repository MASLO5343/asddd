using System;

namespace WpfApp1.Interfaces
{
    /// <summary>
    /// Base interface for a ViewModel that is used in a dialog, returning a boolean result.
    /// </summary>
    public interface IDialogViewModel
    {
        string Title { get; set; }
        bool? DialogResult { get; } // Typically true for OK/Save, false for Cancel, null if closed otherwise
        event Action<bool?> CloseRequested;
        void RequestClose(bool? result);
    }

    /// <summary>
    /// Interface for a ViewModel that is used in a dialog, returning a specific result type.
    /// </summary>
    /// <typeparam name="TDialogResult">The type of the result returned by the dialog.</typeparam>
    public interface IDialogViewModel<TDialogResult>
    {
        string Title { get; set; }
        TDialogResult UserDialogResult { get; } // Property to hold the custom result
        event Action<TDialogResult> CloseRequestedWithResult; // Event to signal closure with a custom result
        void RequestClose(TDialogResult result); // Method to initiate closing with a result

        // Optional: for simple OK/Cancel if TDialogResult can be null
        event Action<bool?> CloseRequested; // Can be used for simple boolean close as well
        void RequestClose(bool? simpleResult);
    }
}