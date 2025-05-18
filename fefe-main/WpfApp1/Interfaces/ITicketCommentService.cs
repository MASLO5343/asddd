// File: WpfApp1/Interfaces/ITicketCommentService.cs
using System.Collections.Generic; // Убедитесь, что using есть
using System.Threading.Tasks;
using WpfApp1.Models;

namespace WpfApp1.Interfaces
{
    public interface ITicketCommentService
    {
        Task<TicketComment> AddCommentAsync(TicketComment comment);
        Task<bool> DeleteCommentAsync(int commentId);
        Task<IEnumerable<TicketComment>> GetCommentsForTicketAsync(int ticketId); // <--- ДОБАВЛЕННАЯ СТРОКА
    }
}