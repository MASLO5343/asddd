using System;
using System.ComponentModel.DataAnnotations; // For [Key]

namespace WpfApp1.Models
{
    public partial class DeviceSoftware
    {
        // ADDED: Single primary key Id as per "Правки.docx"
        [Key]
        public int Id { get; set; }

        // Foreign keys remain
        public int DeviceId { get; set; }
        public virtual Device Device { get; set; } = null!;

        public int SoftwareId { get; set; }
        public virtual Software Software { get; set; } = null!;

        // CHANGED: Renamed InstallDate to InstalledAt and ensured DateTime type as per "Правки.docx"
        public DateTime InstalledAt { get; set; } // Дата установки

        // REMOVED: Notes field as it's not in "Правки.docx" for DeviceSoftware
        // public string? Notes { get; set; }
    }
}