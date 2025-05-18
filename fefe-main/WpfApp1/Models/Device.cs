// fefe-main/WpfApp1/Models/Device.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    public partial class Device
    {
        public Device()
        {
            DeviceSoftwares = new HashSet<DeviceSoftware>();
            Tickets = new HashSet<Ticket>();
            AutomationTasks = new HashSet<AutomationTask>();
        }

        [Key]
        public int DeviceId { get; set; }

        [StringLength(50)]
        public string? InventoryNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        // CHANGED: TypeId to DeviceTypeId for clarity and consistency
        public int? DeviceTypeId { get; set; } // Тип (ПК, сервер, принтер)
        public virtual DeviceType? DeviceType { get; set; } // Navigation property, was Type

        [StringLength(50)]
        public string? IpAddress { get; set; } // Был IPAddress в "Правках"

        [StringLength(50)]
        public string? MacAddress { get; set; } // Был MACAddress в "Правках"

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        // CHANGED: Status to FK + Navigation Property
        public int StatusId { get; set; } // Внешний ключ к DeviceStatus
        public virtual DeviceStatus DeviceStatus { get; set; } = null!; // Navigation property

        public DateOnly? WarrantyUntil { get; set; } // Гарантия до (DATE)

        // CHANGED: OwnerId to ResponsibleUserId for consistency with your DeviceService
        public int? ResponsibleUserId { get; set; } // Ответственное лицо (ID пользователя)
        public virtual User? ResponsibleUser { get; set; } // Navigation property, was Owner

        [StringLength(1000)] // Увеличим длину для Notes, если нужно
        public string? Notes { get; set; } // Дополнительные заметки (остается)

        // Поля CPU, RAM, HDD, LastSeen, CreatedAt, OS УДАЛЕНЫ согласно "Правкам"
        // public string? OS { get; set;} // Это поле есть в вашем DeviceService, но нет в нашей модели Device.cs и "Правках"

        public virtual ICollection<DeviceSoftware> DeviceSoftwares { get; set; }
        public virtual ICollection<Ticket> Tickets { get; set; }
        public virtual ICollection<AutomationTask> AutomationTasks { get; set; }
    }
}