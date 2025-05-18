using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    public partial class User
    {
        public User()
        {
            // Инициализация коллекций, которые мы определили в AppDbContext
            RequestedTickets = new HashSet<Ticket>();
            AssignedTickets = new HashSet<Ticket>();
            TicketCommentsAuthored = new HashSet<TicketComment>(); // CHANGED name for clarity
            LogsRecorded = new HashSet<Log>();                   // CHANGED name for clarity
            // OwnedDevices = new HashSet<Device>(); // Если Device.OwnerId используется
        }

        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = null!;

        [Required]
        // [StringLength(255)] // Или оставить без явной длины, если в БД NVARCHAR(MAX)
        public string PasswordHash { get; set; } = null!;

        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        public int RoleId { get; set; }
        public virtual Role Role { get; set; } = null!;

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Навигационные свойства-коллекции
        public virtual ICollection<Ticket> RequestedTickets { get; set; } // Заявки, где пользователь - заявитель
        public virtual ICollection<Ticket> AssignedTickets { get; set; }  // Заявки, где пользователь - исполнитель
        public virtual ICollection<TicketComment> TicketCommentsAuthored { get; set; } // Комментарии, оставленные пользователем
        public virtual ICollection<Log> LogsRecorded { get; set; } // Логи, связанные с этим пользователем

        // Если есть связь Device.OwnerId -> User.UserId
        // public virtual ICollection<Device> OwnedDevices { get; set; } // Устройства, за которые пользователь ответственен
    }
}