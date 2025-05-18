// File: WpfApp1/Services/Dialogs/AddSoftwareDialogResult.cs (или Models/)
using System;

namespace WpfApp1.Services.Dialogs // или WpfApp1.Models
{
    /// <summary>
    /// Представляет результат диалога добавления ПО к устройству.
    /// </summary>
    public class AddSoftwareDialogResult
    {
        public int? SelectedSoftwareId { get; }
        public DateTime? InstallationDate { get; }
        public bool Confirmed { get; } // Указывает, подтвердил ли пользователь выбор

        public AddSoftwareDialogResult(bool confirmed, int? softwareId = null, DateTime? installationDate = null)
        {
            Confirmed = confirmed;
            SelectedSoftwareId = softwareId;
            InstallationDate = installationDate;
        }
    }
}