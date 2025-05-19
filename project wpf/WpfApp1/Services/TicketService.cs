// fefe-main/WpfApp1/Services/TicketService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context; // Для LogContext
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WpfApp1.Interfaces;
using WpfApp1.Models;
using WpfApp1.Services.Data; // Для TicketFilterParameters

namespace WpfApp1.Services
{
    public class TicketService : ITicketService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TicketService> _logger;
        private readonly IApplicationStateService _applicationStateService;
        // private readonly IIpAddressProvider _ipAddressProvider; // EXAMPLE

        public TicketService(AppDbContext context, ILogger<TicketService> logger, IApplicationStateService applicationStateService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService));
        }

        private void SetupLogContext(string actionName, string? targetEntityInfo = null)
        {
            var currentUser = _applicationStateService.CurrentUser;
            LogContext.PushProperty("UserId", currentUser?.UserId);
            LogContext.PushProperty("UserName", currentUser?.Username);
            LogContext.PushProperty("Action", actionName);
            LogContext.PushProperty("TableAffected", "Tickets");
            // LogContext.PushProperty("IPAddress", _ipAddressProvider?.GetLocalIpAddress()); // EXAMPLE
            // EntityId не пишем в отдельную колонку Log.cs
        }

        public async Task<Ticket> AddTicketAsync(Ticket ticket)
        {
            SetupLogContext("CreateTicket", $"Title: {ticket.Title}");
            if (ticket == null)
            {
                _logger.LogError("Попытка создать null объект заявки.");
                throw new ArgumentNullException(nameof(ticket));
            }

            // Проверка обязательных полей (Title, Description, Category уже non-nullable в модели и должны быть string.Empty по умолчанию)
            // но для бизнес-логики лучше проверить на осмысленные значения
            if (string.IsNullOrWhiteSpace(ticket.Title))
                throw new ArgumentException("Заголовок заявки не может быть пустым.", nameof(ticket.Title));
            if (string.IsNullOrWhiteSpace(ticket.Description))
                throw new ArgumentException("Описание заявки не может быть пустым.", nameof(ticket.Description));
            if (string.IsNullOrWhiteSpace(ticket.Category)) // Новое обязательное поле
                throw new ArgumentException("Категория заявки не может быть пустой.", nameof(ticket.Category));


            ticket.CreatedAt = DateTime.UtcNow;
            ticket.UpdatedAt = null; // Явно при создании
            ticket.RequesterId = _applicationStateService.CurrentUser?.UserId ??
                                   throw new InvalidOperationException("Невозможно создать заявку без авторизованного пользователя (RequesterId).");

            // Убедимся, что TicketId сбрасывается (EF Core обычно сам это делает для Identity PK)
            ticket.TicketId = 0;

            // StatusId и PriorityId должны быть установлены до вызова этого метода
            // или иметь значения по умолчанию, если это применимо (например, "Открыта", "Средний")
            if (ticket.StatusId == 0) throw new ArgumentException("Статус заявки должен быть указан.", nameof(ticket.StatusId));
            if (ticket.PriorityId == 0) throw new ArgumentException("Приоритет заявки должен быть указан.", nameof(ticket.PriorityId));


            _logger.LogInformation("Попытка добавить новую заявку: {TicketTitle} от пользователя ID {RequesterId}", ticket.Title, ticket.RequesterId);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Tickets.AddAsync(ticket);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Заявка успешно добавлена: {TicketTitle}, ID: {TicketId}", ticket.Title, ticket.TicketId);
                return ticket;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при добавлении заявки: {TicketTitle}", ticket.Title);
                throw;
            }
        }

        public async Task<bool> DeleteTicketAsync(int ticketId)
        {
            SetupLogContext("DeleteTicket", $"TicketId: {ticketId}");
            _logger.LogInformation("Попытка удалить заявку с ID: {TicketId}", ticketId);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var ticket = await _context.Tickets
                                     .Include(t => t.TicketComments) // Включаем комментарии для удаления
                                     .FirstOrDefaultAsync(t => t.TicketId == ticketId);
                if (ticket == null)
                {
                    _logger.LogWarning("Заявка с ID: {TicketId} не найдена для удаления.", ticketId);
                    await transaction.RollbackAsync();
                    return false;
                }

                // Удаление связанных комментариев (ваш код уже это делает, что хорошо)
                if (ticket.TicketComments != null && ticket.TicketComments.Any())
                {
                    _logger.LogInformation("Удаление {CommentCount} комментариев, связанных с заявкой ID: {TicketId}", ticket.TicketComments.Count, ticketId);
                    _context.TicketComments.RemoveRange(ticket.TicketComments);
                }

                _context.Tickets.Remove(ticket);
                await _context.SaveChangesAsync(); // Это изменение также должно быть частью транзакции
                await transaction.CommitAsync();
                _logger.LogInformation("Заявка с ID: {TicketId} и связанные комментарии успешно удалены.", ticketId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при удалении заявки с ID: {TicketId}", ticketId);
                throw;
            }
        }

        public async Task<IEnumerable<Ticket>> GetAllTicketsAsync(TicketFilterParameters? filters, string? sortBy, bool ascending)
        {
            SetupLogContext("GetAllTicketsFiltered", filters != null ? $"Filters: {filters.SearchText}, Status: {filters.StatusId}" : "No filters"); // Пример логирования фильтров
            _logger.LogInformation("Получение всех заявок с фильтрами и сортировкой.");
            try
            {
                var query = _context.Tickets
                                    .Include(t => t.Status)         // Навигационное свойство
                                    .Include(t => t.Priority)       // Навигационное свойство
                                    .Include(t => t.Requester)      // CHANGED: было CreatedByUser
                                    .Include(t => t.Assignee)       // CHANGED: было AssignedToUser
                                    .Include(t => t.Device)
                                    .AsQueryable();

                if (filters != null)
                {
                    if (!string.IsNullOrWhiteSpace(filters.SearchText))
                    {
                        string searchTextLower = filters.SearchText.ToLower();
                        query = query.Where(t => (t.Title.ToLower().Contains(searchTextLower)) ||
                                                 (t.Description.ToLower().Contains(searchTextLower)) ||
                                                 (t.Category.ToLower().Contains(searchTextLower))); // ADDED: Search by Category
                    }
                    if (filters.StatusId.HasValue)
                    {
                        query = query.Where(t => t.StatusId == filters.StatusId.Value);
                    }
                    if (filters.PriorityId.HasValue)
                    {
                        query = query.Where(t => t.PriorityId == filters.PriorityId.Value);
                    }
                    if (filters.AssignedToUserId.HasValue) // Используем имя из фильтра, маппим на AssigneeId
                    {
                        query = query.Where(t => t.AssigneeId == filters.AssignedToUserId.Value);
                    }
                    if (filters.CreatedByUserId.HasValue) // Используем имя из фильтра, маппим на RequesterId
                    {
                        query = query.Where(t => t.RequesterId == filters.CreatedByUserId.Value);
                    }
                    if (filters.DeviceId.HasValue)
                    {
                        query = query.Where(t => t.DeviceId == filters.DeviceId.Value);
                    }
                    if (filters.DateFrom.HasValue)
                    {
                        query = query.Where(t => t.CreatedAt >= filters.DateFrom.Value);
                    }
                    if (filters.DateTo.HasValue)
                    {
                        query = query.Where(t => t.CreatedAt < filters.DateTo.Value.AddDays(1));
                    }
                    // ADDED: Filter by Category if present in TicketFilterParameters
                    // if (!string.IsNullOrWhiteSpace(filters.CategoryName))
                    // {
                    //     query = query.Where(t => t.Category.ToLower() == filters.CategoryName.ToLower());
                    // }
                }

                query = ApplySorting(query, sortBy, ascending);
                var tickets = await query.ToListAsync();
                _logger.LogInformation("Успешно получено {TicketCount} заявок.", tickets.Count);
                return tickets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении всех заявок.");
                throw;
            }
        }

        private IQueryable<Ticket> ApplySorting(IQueryable<Ticket> query, string? sortBy, bool ascending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                sortBy = "CreatedAt";
                ascending = false;
            }

            // Приводим sortBy к нижнему регистру для надежного сравнения
            string sortByLower = sortBy.ToLowerInvariant();

            Expression<Func<Ticket, object>>? keySelector = sortByLower switch
            {
                "status" or "status.statusname" => t => t.Status != null ? (object)t.Status.StatusName : "",
                "priority" or "priority.priorityname" => t => t.Priority != null ? (object)t.Priority.PriorityName : "",
                "createdat" => t => t.CreatedAt,
                "deadline" => t => (object?)t.Deadline ?? DateTime.MaxValue,
                "assignedtouser" or "assignedtouser.fullname" => t => t.Assignee != null ? (object)t.Assignee.FullName! : "", // CHANGED: t.Assignee
                "createdbyuser.fullname" or "requester.fullname" => t => t.Requester != null ? (object)t.Requester.FullName! : "", // CHANGED: t.Requester
                "device.name" => t => t.Device != null ? (object)t.Device.Name! : "",
                "title" => t => t.Title, // Title non-nullable
                "category" => t => t.Category, // ADDED: Sort by Category
                "id" or "ticketid" => t => t.TicketId,
                _ => t => t.CreatedAt
            };
            // Добавил явное приведение к object для строковых полей, чтобы избежать проблем с EF Core LINQ провайдером
            // и добавил ! (null-forgiving operator) для FullName, так как мы проверяем на null родительский объект.

            if (keySelector != null) // Убедимся, что keySelector был назначен
            {
                if (ascending)
                {
                    query = query.OrderBy(keySelector).ThenBy(t => t.TicketId);
                }
                else
                {
                    query = query.OrderByDescending(keySelector).ThenByDescending(t => t.TicketId);
                }
            }
            // Если keySelector не был назначен (например, из-за некорректного sortBy), можно оставить исходный query или применить сортировку по умолчанию
            // В текущей логике _ => t => t.CreatedAt уже обрабатывает это.

            return query;
        }

        public async Task<IEnumerable<TicketPriority>> GetAvailablePrioritiesAsync()
        {
            SetupLogContext("GetAvailablePriorities");
            _logger.LogInformation("Получение доступных приоритетов заявок.");
            try
            {
                return await _context.TicketPriorities.OrderBy(p => p.SortOrder).ThenBy(p => p.PriorityName).ToListAsync(); // Сортировка по SortOrder
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении доступных приоритетов заявок.");
                throw;
            }
        }

        public async Task<IEnumerable<TicketStatus>> GetAvailableStatusesAsync()
        {
            SetupLogContext("GetAvailableStatuses");
            _logger.LogInformation("Получение доступных статусов заявок.");
            try
            {
                return await _context.TicketStatuses.OrderBy(s => s.SortOrder).ThenBy(s => s.StatusName).ToListAsync(); // Сортировка по SortOrder
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении доступных статусов заявок.");
                throw;
            }
        }

        public async Task<Ticket?> GetTicketByIdAsync(int ticketId)
        {
            SetupLogContext("GetTicketById", $"TicketId: {ticketId}");
            _logger.LogInformation("Получение заявки по ID: {TicketId} с деталями.", ticketId);
            try
            {
                var ticket = await _context.Tickets
                                           .Include(t => t.Status)
                                           .Include(t => t.Priority)
                                           .Include(t => t.Requester) // CHANGED
                                           .Include(t => t.Assignee)  // CHANGED
                                           .Include(t => t.Device)
                                           .Include(t => t.TicketComments)
                                               .ThenInclude(tc => tc.Author) // Автор комментария
                                           .FirstOrDefaultAsync(t => t.TicketId == ticketId);
                if (ticket == null)
                {
                    _logger.LogWarning("Заявка с ID {TicketId} не найдена.", ticketId);
                }
                else
                {
                    _logger.LogInformation("Заявка с ID {TicketId} успешно получена.", ticketId);
                }
                return ticket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении заявки по ID: {TicketId}", ticketId);
                throw;
            }
        }

        public async Task<Ticket?> UpdateTicketAsync(Ticket ticket) // Возвращает обновленный Ticket или null, если не найден
        {
            SetupLogContext("UpdateTicket", $"TicketId: {ticket.TicketId}");
            if (ticket == null)
            {
                _logger.LogError("Попытка обновить null объект заявки.");
                throw new ArgumentNullException(nameof(ticket));
            }

            // Проверки на обязательные поля
            if (string.IsNullOrWhiteSpace(ticket.Title)) throw new ArgumentException("Заголовок заявки не может быть пустым.", nameof(ticket.Title));
            if (string.IsNullOrWhiteSpace(ticket.Description)) throw new ArgumentException("Описание заявки не может быть пустым.", nameof(ticket.Description));
            if (string.IsNullOrWhiteSpace(ticket.Category)) throw new ArgumentException("Категория заявки не может быть пустой.", nameof(ticket.Category));
            if (ticket.StatusId == 0) throw new ArgumentException("Статус заявки должен быть указан.", nameof(ticket.StatusId));
            if (ticket.PriorityId == 0) throw new ArgumentException("Приоритет заявки должен быть указан.", nameof(ticket.PriorityId));


            _logger.LogInformation("Попытка обновить заявку с ID: {TicketId}", ticket.TicketId);
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingTicket = await _context.Tickets.FindAsync(ticket.TicketId);
                if (existingTicket == null)
                {
                    _logger.LogWarning("Заявка с ID: {TicketId} не найдена для обновления.", ticket.TicketId);
                    await transaction.RollbackAsync();
                    return null;
                }

                // Обновляем поля, не затрагивая CreatedAt и RequesterId (бывший CreatedByUserId)
                existingTicket.Title = ticket.Title;
                existingTicket.Description = ticket.Description;
                existingTicket.Category = ticket.Category; // Обновляем категорию
                existingTicket.StatusId = ticket.StatusId;
                existingTicket.PriorityId = ticket.PriorityId;
                existingTicket.AssigneeId = ticket.AssigneeId;
                existingTicket.DeviceId = ticket.DeviceId;
                existingTicket.Deadline = ticket.Deadline; // Обновляем Deadline
                existingTicket.UpdatedAt = DateTime.UtcNow;

                // _context.Entry(existingTicket).CurrentValues.SetValues(ticket); // Этот метод может перезаписать CreatedAt и RequesterId, поэтому используем ручное присвоение

                // _context.Entry(existingTicket).State = EntityState.Modified; // Не обязательно, если existingTicket отслеживается

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Заявка успешно обновлена: ID: {TicketId}", existingTicket.TicketId);
                return existingTicket;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Конфликт параллелизма при обновлении заявки ID: {TicketId}", ticket.TicketId);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при обновлении заявки ID: {TicketId}", ticket.TicketId);
                throw;
            }
        }
    }
}

// Необходимые изменения в TicketFilterParameters.cs, если его структура отличается:
// public class TicketFilterParameters
// {
//     public string? SearchText { get; set; }
//     public int? StatusId { get; set; } // Используем StatusId
//     public int? PriorityId { get; set; } // Используем PriorityId
//     public int? CreatedByUserId { get; set; } // В вашем коде, будет маппиться на RequesterId
//     public int? AssignedToUserId { get; set; } // В вашем коде, будет маппиться на AssigneeId
//     public int? DeviceId { get; set; }
//     public DateTime? DateFrom { get; set; }
//     public DateTime? DateTo { get; set; }
//     public string? CategoryName { get; set; } // Для фильтрации по новой категории
// }