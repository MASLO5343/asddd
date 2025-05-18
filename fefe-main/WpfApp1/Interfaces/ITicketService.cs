// File: WpfApp1/Interfaces/ITicketService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WpfApp1.Models;
using WpfApp1.Services.Data; // Используем созданный класс DTO/Filter

namespace WpfApp1.Interfaces
{
    public interface ITicketService
    {
        /// <summary>
        /// Получает список заявок с применением фильтрации и сортировки.
        /// </summary>
        /// <param name="filters">Параметры фильтрации.</param>
        /// <param name="sortBy">Поле для сортировки (например, "CreatedAt", "Priority.PriorityName").</param>
        /// <param name="ascending">Направление сортировки (true = по возрастанию).</param>
        /// <returns>Коллекция отфильтрованных и отсортированных заявок.</returns>
        Task<IEnumerable<Ticket>> GetAllTicketsAsync(TicketFilterParameters? filters, string? sortBy, bool ascending);

        /// <summary>
        /// Получает одну заявку по ID со всеми связанными данными (комментарии, автор, исполнитель и т.д.).
        /// </summary>
        /// <param name="ticketId">Идентификатор заявки.</param>
        /// <returns>Заявка или null, если не найдена.</returns>
        Task<Ticket?> GetTicketByIdAsync(int ticketId);

        /// <summary>
        /// Добавляет новую заявку.
        /// </summary>
        /// <param name="ticket">Объект заявки.</param>
        /// <returns>Созданная заявка с присвоенным ID и датой создания.</returns>
        Task<Ticket> AddTicketAsync(Ticket ticket);

        /// <summary>
        /// Обновляет существующую заявку.
        /// </summary>
        /// <param name="ticket">Объект заявки с обновленными данными.</param>
        /// <returns>Обновленная заявка или null, если заявка не найдена.</returns>
        Task<Ticket?> UpdateTicketAsync(Ticket ticket);

        /// <summary>
        /// Удаляет заявку по ID.
        /// </summary>
        /// <param name="ticketId">Идентификатор заявки.</param>
        /// <returns>True, если удаление успешно, иначе false.</returns>
        Task<bool> DeleteTicketAsync(int ticketId);

        /// <summary>
        /// Получает список всех доступных статусов заявок.
        /// </summary>
        Task<IEnumerable<TicketStatus>> GetAvailableStatusesAsync();

        /// <summary>
        /// Получает список всех доступных приоритетов заявок.
        /// </summary>
        Task<IEnumerable<TicketPriority>> GetAvailablePrioritiesAsync();
    }
}