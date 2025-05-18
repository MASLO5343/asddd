using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WpfApp1.Interfaces;
using WpfApp1.Models;
using WpfApp1.Services.Data;
using Serilog.Context; // For LogContext

namespace WpfApp1.Services
{
    public class LogService : ILogService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LogService> _logger;

        public LogService(AppDbContext context, ILogger<LogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Log>> GetLogsAsync(LogFilterParameters filters, int pageNumber, int pageSize)
        {
            // LogContext is generally for *writing* logs, not reading them.
            // We can log the call to this method itself.
            _logger.LogDebug("Запрос списка логов: страница {PageNumber}, размер {PageSize}. Фильтры: {@Filters}", pageNumber, pageSize, filters);

            var query = _context.Logs.AsQueryable();

            if (filters != null)
            {
                if (filters.FromDate.HasValue)
                    query = query.Where(l => l.TimeStamp >= filters.FromDate.Value);
                if (filters.ToDate.HasValue)
                    query = query.Where(l => l.TimeStamp <= filters.ToDate.Value.AddDays(1).AddTicks(-1)); // Include whole ToDate
                if (!string.IsNullOrWhiteSpace(filters.Level))
                    query = query.Where(l => l.Level == filters.Level);

                if (filters.UserId.HasValue) // Assuming UserId is a direct property on Log model
                    query = query.Where(l => l.UserId == filters.UserId.Value);

                if (!string.IsNullOrWhiteSpace(filters.UserName)) // Assuming UserName is a direct property
                    query = query.Where(l => l.UserName != null && l.UserName.Contains(filters.UserName));

                if (!string.IsNullOrWhiteSpace(filters.Action)) // Assuming Action is a direct property
                    query = query.Where(l => l.Action != null && l.Action.Contains(filters.Action));

                if (!string.IsNullOrWhiteSpace(filters.SearchText))
                {
                    var term = filters.SearchText.ToLower();
                    query = query.Where(l => (l.Message != null && l.Message.ToLower().Contains(term)) ||
                                             (l.Exception != null && l.Exception.ToLower().Contains(term)) ||
                                             (l.Properties != null && l.Properties.ToLower().Contains(term)) // Search in XML/JSON properties field
                                             );
                }
            }

            // Ensure pageNumber and pageSize are valid
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20; // Default page size

            try
            {
                return await query.OrderByDescending(l => l.TimeStamp)
                                  .Skip((pageNumber - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка логов. Фильтры: {@Filters}, Страница: {Page}, Размер: {PageSize}", filters, pageNumber, pageSize);
                throw; // Re-throw to allow higher level handling or return empty list
            }
        }

        public async Task<int> GetLogsCountAsync(LogFilterParameters filters)
        {
            _logger.LogDebug("Запрос общего количества логов. Фильтры: {@Filters}", filters);
            var query = _context.Logs.AsQueryable();

            if (filters != null)
            {
                if (filters.FromDate.HasValue)
                    query = query.Where(l => l.TimeStamp >= filters.FromDate.Value);
                if (filters.ToDate.HasValue)
                    query = query.Where(l => l.TimeStamp <= filters.ToDate.Value.AddDays(1).AddTicks(-1));
                if (!string.IsNullOrWhiteSpace(filters.Level))
                    query = query.Where(l => l.Level == filters.Level);
                if (filters.UserId.HasValue)
                    query = query.Where(l => l.UserId == filters.UserId.Value);
                if (!string.IsNullOrWhiteSpace(filters.UserName))
                    query = query.Where(l => l.UserName != null && l.UserName.Contains(filters.UserName));
                if (!string.IsNullOrWhiteSpace(filters.Action))
                    query = query.Where(l => l.Action != null && l.Action.Contains(filters.Action));
                if (!string.IsNullOrWhiteSpace(filters.SearchText))
                {
                    var term = filters.SearchText.ToLower();
                    query = query.Where(l => (l.Message != null && l.Message.ToLower().Contains(term)) ||
                                            (l.Exception != null && l.Exception.ToLower().Contains(term)) ||
                                            (l.Properties != null && l.Properties.ToLower().Contains(term))
                                            );
                }
            }
            try
            {
                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при подсчете количества логов. Фильтры: {@Filters}", filters);
                throw;
            }
        }
    }
}