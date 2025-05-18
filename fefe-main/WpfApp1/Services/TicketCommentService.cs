// fefe-main/WpfApp1/Services/TicketCommentService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context; // Для LogContext
using System;
using System.Collections.Generic; // Для IEnumerable
using System.Linq; // Для OrderBy
using System.Threading.Tasks;
using WpfApp1.Interfaces;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class TicketCommentService : ITicketCommentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TicketCommentService> _logger;
        private readonly IApplicationStateService _applicationStateService;
        // private readonly IIpAddressProvider _ipAddressProvider; // EXAMPLE

        public TicketCommentService(AppDbContext context, ILogger<TicketCommentService> logger, IApplicationStateService applicationStateService)
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
            LogContext.PushProperty("TableAffected", "TicketComments");
            // LogContext.PushProperty("IPAddress", _ipAddressProvider?.GetLocalIpAddress()); // EXAMPLE
        }

        public async Task<TicketComment?> AddCommentAsync(TicketComment comment) // Возвращаем TicketComment? для единообразия
        {
            // Используем TicketId и начало текста комментария для контекста
            SetupLogContext("AddComment", $"TicketId: {comment?.TicketId}, CommentStart: {comment?.Comment?.Substring(0, Math.Min(comment.Comment?.Length ?? 0, 30))}");
            var currentUser = _applicationStateService.CurrentUser;

            if (comment == null)
            {
                _logger.LogError("Попытка добавить null объект комментария.");
                throw new ArgumentNullException(nameof(comment));
            }
            if (comment.TicketId <= 0)
            {
                _logger.LogWarning("Некорректный TicketId ({TicketId}) при добавлении комментария.", comment.TicketId);
                throw new ArgumentException("TicketId должен быть установлен и быть больше нуля.", nameof(comment.TicketId));
            }
            // Валидация текста комментария (поле Comment согласно нашей модели)
            if (string.IsNullOrWhiteSpace(comment.Comment))
            {
                _logger.LogWarning("Попытка добавить пустой комментарий к заявке ID: {TicketId} пользователем {CurrentUsername}.",
                    comment.TicketId, currentUser?.Username ?? "System");
                throw new ArgumentException("Текст комментария не может быть пустым.", nameof(comment.Comment));
            }

            comment.CreatedAt = DateTime.UtcNow;
            comment.AuthorId = currentUser?.UserId ??
                               throw new InvalidOperationException("Невозможно добавить комментарий без авторизованного пользователя (AuthorId).");

            // Убедимся, что TicketCommentId (PK нашей модели) сброшен перед добавлением
            comment.TicketCommentId = 0; // Используем TicketCommentId из нашей модели TicketComment.cs

            _logger.LogInformation("Попытка добавить новый комментарий к заявке ID: {TicketId} пользователем {CurrentUsername} (ID: {CurrentUserId})",
                comment.TicketId, currentUser?.Username ?? "System", currentUser?.UserId);

            await using var transaction = await _context.Database.BeginTransactionAsync(); // Сохраняем транзакцию из вашего кода
            try
            {
                await _context.TicketComments.AddAsync(comment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Загрузка связанного автора (User) для возврата полного объекта
                // Ваш код уже делает это, что хорошо.
                await _context.Entry(comment).Reference(c => c.Author).LoadAsync();

                _logger.LogInformation("Комментарий успешно добавлен к заявке ID: {TicketId}. ID нового комментария: {TicketCommentId}. Автор: {AuthorUsername} (ID: {AuthorId})",
                    comment.TicketId, comment.TicketCommentId, comment.Author?.Username ?? "N/A", comment.AuthorId);

                return comment;
            }
            catch (DbUpdateException ex) // Например, если TicketId не существует
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка базы данных при добавлении комментария к заявке ID: {TicketId}. Убедитесь, что заявка существует.", comment.TicketId);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при добавлении комментария к заявке ID: {TicketId}", comment.TicketId);
                throw;
            }
        }

        public async Task<bool> DeleteCommentAsync(int ticketCommentId) // Используем TicketCommentId как PK
        {
            SetupLogContext("DeleteComment", $"TicketCommentId: {ticketCommentId}");
            var currentUser = _applicationStateService.CurrentUser;
            var currentUserId = currentUser?.UserId;

            _logger.LogInformation("Попытка удалить комментарий с ID: {TicketCommentId} пользователем {CurrentUsername} (ID: {CurrentUserId})",
                ticketCommentId, currentUser?.Username ?? "System", currentUserId);

            await using var transaction = await _context.Database.BeginTransactionAsync(); // Сохраняем транзакцию из вашего кода
            try
            {
                // Ищем комментарий по TicketCommentId
                var commentToDelete = await _context.TicketComments.FindAsync(ticketCommentId);
                if (commentToDelete == null)
                {
                    _logger.LogWarning("Комментарий с ID: {TicketCommentId} не найден для удаления.", ticketCommentId);
                    await transaction.RollbackAsync();
                    return false;
                }

                // Placeholder для проверки прав на удаление
                // bool canDelete = (commentToDelete.AuthorId == currentUserId) || (_applicationStateService.UserHasRole("Admin")); // Пример
                // if (!canDelete)
                // {
                //    _logger.LogWarning("Пользователь {CurrentUsername} (ID: {CurrentUserId}) не имеет прав на удаление комментария ID: {TicketCommentId} (Автор ID: {AuthorId})",
                //        currentUser?.Username ?? "System", currentUserId, ticketCommentId, commentToDelete.AuthorId);
                //    await transaction.RollbackAsync();
                //    // Можно бросить UnauthorizedAccessException или вернуть специальный статус/false
                //    throw new UnauthorizedAccessException("У вас нет прав на удаление этого комментария."); 
                // }

                _context.TicketComments.Remove(commentToDelete);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Комментарий с ID: {TicketCommentId} (принадлежавший заявке ID: {TicketId}, автор ID: {AuthorId}) успешно удален пользователем {CurrentUsername}.",
                    ticketCommentId, commentToDelete.TicketId, commentToDelete.AuthorId, currentUser?.Username ?? "System");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при удалении комментария с ID: {TicketCommentId}", ticketCommentId);
                throw; // или return false;
            }
        }

        // Новый метод для получения всех комментариев для конкретной заявки
        public async Task<IEnumerable<TicketComment>> GetCommentsForTicketAsync(int ticketId)
        {
            SetupLogContext("GetCommentsForTicket", $"TicketId: {ticketId}");
            _logger.LogInformation("Запрос комментариев для заявки ID: {TicketId}", ticketId);
            try
            {
                var comments = await _context.TicketComments
                                         .Where(tc => tc.TicketId == ticketId)
                                         .Include(tc => tc.Author) // Включаем информацию об авторе (User)
                                         .OrderBy(tc => tc.CreatedAt) // Сначала старые комментарии
                                         .ToListAsync();
                _logger.LogInformation("Успешно получено {CommentCount} комментариев для заявки ID: {TicketId}", comments.Count, ticketId);
                return comments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении комментариев для заявки ID: {TicketId}", ticketId);
                throw; // или вернуть пустой список
            }
        }
    }
}