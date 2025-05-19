// fefe-main/WpfApp1/Models/Ticket.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    public partial class Ticket // Используем partial, если у вас есть автогенерируемая часть
    {
        public Ticket()
        {
            TicketComments = new HashSet<TicketComment>();
        }

        [Key]
        public int TicketId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? Deadline { get; set; } // Поле Deadline есть в вашем ApplySorting

        // Используем RequesterId и AssigneeId в соответствии с нашей предыдущей моделью User и связями
        public int RequesterId { get; set; } // Соответствует вашему CreatedByUserId
        public virtual User Requester { get; set; } = null!; // Соответствует вашему CreatedByUser

        public int? AssigneeId { get; set; } // Соответствует вашему AssignedToUserId
        public virtual User? Assignee { get; set; } // Соответствует вашему AssignedToUser

        // Внешние ключи и навигационные свойства для Status и Priority
        public int StatusId { get; set; } // Внешний ключ к TicketStatus
        public virtual TicketStatus Status { get; set; } = null!;

        public int PriorityId { get; set; } // Внешний ключ к TicketPriority
        public virtual TicketPriority Priority { get; set; } = null!;

        public int? DeviceId { get; set; }
        public virtual Device? Device { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty; // Добавлено согласно "Правкам"

        public virtual ICollection<TicketComment> TicketComments { get; set; }
    }
}