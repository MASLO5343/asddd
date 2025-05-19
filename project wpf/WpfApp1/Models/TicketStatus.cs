// fefe-main/WpfApp1/Models/TicketStatus.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    public class TicketStatus // Ваш TicketService.cs использует это имя класса
    {
        public TicketStatus()
        {
            Tickets = new HashSet<Ticket>();
        }

        [Key]
        public int StatusId { get; set; } // Имя PK, которое вы, вероятно, используете (или TicketStatusId)

        [Required]
        [StringLength(50)]
        public string StatusName { get; set; } = string.Empty;

        // Опционально: порядок сортировки для статусов
        public int SortOrder { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}