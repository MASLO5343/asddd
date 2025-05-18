using System;
using System.ComponentModel.DataAnnotations; // For [Required]

namespace WpfApp1.Models
{
    public partial class TicketComment
    {
        public int TicketCommentId { get; set; }
        public int TicketId { get; set; }

        // CHANGED: Renamed UserId to AuthorId as per "Правки.docx"
        public int AuthorId { get; set; } // Внешний ключ к User (был UserId)

        // CHANGED: Renamed CommentText to Comment as per "Правки.docx"
        [Required]
        public string Comment { get; set; } = null!; // Текст комментария (был CommentText)

        public DateTime CreatedAt { get; set; }

        // CHANGED: Navigation property name adjusted to reflect AuthorId if desired, 
        // or keep User if the FK property name change is enough.
        // EF Core can map AuthorId to User. For clarity, the FK property itself was renamed.
        public virtual User Author { get; set; } = null!; // Автор комментария (был User, но связан с AuthorId)
        public virtual Ticket Ticket { get; set; } = null!;
    }
}