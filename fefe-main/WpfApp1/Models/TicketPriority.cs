// fefe-main/WpfApp1/Models/TicketPriority.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    public class TicketPriority // Ваш TicketService.cs использует это имя класса
    {
        public TicketPriority()
        {
            Tickets = new HashSet<Ticket>();
        }

        [Key]
        public int PriorityId { get; set; } // Имя PK, которое вы, вероятно, используете (или TicketPriorityId)

        [Required]
        [StringLength(50)]
        public string PriorityName { get; set; } = string.Empty;

        // Опционально: порядок сортировки для приоритетов (например, Критический=1, Высокий=2 и т.д.)
        public int SortOrder { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}