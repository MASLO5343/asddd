using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WpfApp1.Models
{
    public partial class Log
    {
        public int Id { get; set; } // Standard Serilog PK

        public string Message { get; set; } = null!; // Standard Serilog

        [StringLength(128)]
        public string Level { get; set; } = null!; // Standard Serilog

        public DateTime TimeStamp { get; set; } // Standard Serilog

        public string? Exception { get; set; } // Standard Serilog

        [Column(TypeName = "xml")]
        public string? Properties { get; set; } // Standard Serilog, for other non-column properties

        public int? UserId { get; set; } // Custom - соответствует Logs.UserId -> Users.UserId из "Правок"
        public virtual User? User { get; set; } // ADDED: Navigation property based on "Правки" (Logs.UserId → Users.UserId)

        [StringLength(128)]
        public string? UserName { get; set; } // Custom - для удобства, может заполняться из User

        [StringLength(255)]
        public string? Action { get; set; } // Custom - для описания действия

        // REMOVED: EntityType and EntityId as per "Правки.docx" for Log table
        // public string? EntityType { get; set; } 
        // public string? EntityId { get; set; } 

        // ADDED: TableAffected as per "Правки.docx" (пункт 2 вашего анализа)
        [StringLength(50)]
        public string? TableAffected { get; set; }

        // ADDED: IPAddress as per "Правки.docx" (пункт 1 вашего анализа и раздел "Дополнительно" в Правках)
        [StringLength(50)] // Предполагаемая длина для IP-адреса
        public string? IPAddress { get; set; }
    }
}