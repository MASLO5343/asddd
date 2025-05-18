using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    public partial class Software
    {
        public Software()
        {
            DeviceSoftwares = new HashSet<DeviceSoftware>();
        }

        [Key]
        public int SoftwareId { get; set; } // Как в AppDbContext

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(50)]
        public string? Version { get; set; }

        [StringLength(100)] // Как в AppDbContext
        public string? Vendor { get; set; } // Производитель

        [StringLength(255)] // Как в AppDbContext
        public string? LicenseKey { get; set; }

        public DateTime? PurchaseDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? Seats { get; set; } // Количество лицензированных мест

        // [Column(TypeName = "nvarchar(max)")] // Атрибут здесь не нужен, если конфигурируем через Fluent API
        public string? Notes { get; set; } // Дополнительные заметки

        public virtual ICollection<DeviceSoftware> DeviceSoftwares { get; set; }
    }
}