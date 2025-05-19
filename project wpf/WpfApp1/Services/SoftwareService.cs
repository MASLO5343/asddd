// fefe-main/WpfApp1/Services/SoftwareService.cs
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

// Предполагается, что SoftwareFilterParameters будет создан или адаптирован
// using WpfApp1.Services.Data; 

// Предварительное определение DTO для фильтрации, если потребуется
// Если у вас есть файл WpfApp1/Services/Data/SoftwareFilterParameters.cs, этот класс можно удалить отсюда
// и использовать using WpfApp1.Services.Data;
public class SoftwareFilterParameters // Если это внешний класс, то public не нужен здесь
{
    public string? SearchTerm { get; set; }
    public string? Vendor { get; set; }
    // Можно добавить другие поля для фильтрации, например, HasExpiryDateSoon, IsLicenseExpired etc.
}

// ... (другие using директивы и код класса)

namespace WpfApp1.Services
{
    public class SoftwareService : ISoftwareService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SoftwareService> _logger;
        private readonly IApplicationStateService _applicationStateService;
        // private readonly IIpAddressProvider _ipAddressProvider; // EXAMPLE

        public SoftwareService(AppDbContext context, ILogger<SoftwareService> logger, IApplicationStateService applicationStateService)
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
            LogContext.PushProperty("TableAffected", "Software");
            // LogContext.PushProperty("IPAddress", _ipAddressProvider?.GetLocalIpAddress()); // EXAMPLE
        }

        // Существующий метод с параметрами
        public async Task<IEnumerable<Software>> GetAllSoftwareAsync(SoftwareFilterParameters? filters, string? sortBy = null, bool ascending = true, int pageNumber = 1, int pageSize = 20)
        {
            SetupLogContext("GetAllSoftware_Filtered", filters != null ? $"Search: {filters.SearchTerm}, Vendor: {filters.Vendor}" : "No filters");
            _logger.LogDebug("Запрос списка ПО. Фильтры: {@Filters}, Сортировка: {SortBy} {Ascending}, Стр: {Page}, Размер: {PageSize}", filters, sortBy, ascending, pageNumber, pageSize);

            try
            {
                var query = _context.Software.AsQueryable();

                if (filters != null)
                {
                    if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                    {
                        var term = filters.SearchTerm.ToLower();
                        query = query.Where(s =>
                            (s.Name.ToLower().Contains(term)) ||
                            (s.Version != null && s.Version.ToLower().Contains(term)) ||
                            (s.Vendor != null && s.Vendor.ToLower().Contains(term)) ||
                            (s.LicenseKey != null && s.LicenseKey.ToLower().Contains(term)) ||
                            (s.Notes != null && s.Notes.ToLower().Contains(term))
                        );
                    }
                    if (!string.IsNullOrWhiteSpace(filters.Vendor))
                    {
                        query = query.Where(s => s.Vendor != null && s.Vendor.ToLower().Contains(filters.Vendor.ToLower()));
                    }
                }

                query = ApplySoftwareSorting(query, sortBy, ascending); // Предполагается, что ApplySoftwareSorting существует

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10; // Или ваше значение по умолчанию
                if (pageSize > 100) pageSize = 100; // Ограничение максимального размера страницы

                var softwareList = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
                _logger.LogInformation("Успешно получено {SoftwareCount} записей о ПО.", softwareList.Count);
                return softwareList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка ПО с фильтрами.");
                throw;
            }
        }

        // >>> НАЧАЛО ДОБАВЛЕННОГО КОДА <<<
        /// <summary>
        /// Получает весь список программного обеспечения без фильтрации и пагинации.
        /// </summary>
        /// <returns>Коллекция всего программного обеспечения.</returns>
        public async Task<IEnumerable<Software>> GetAllSoftwareAsync()
        {
            SetupLogContext("GetAllSoftware_Parameterless");
            _logger.LogInformation("Запрос всего списка ПО (без фильтров и пагинации).");
            try
            {
                // Просто возвращаем все записи из таблицы Software, отсортированные по имени
                return await _context.Software
                                     .OrderBy(s => s.Name) // Опциональная сортировка по умолчанию
                                     .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении всего списка ПО.");
                // В зависимости от политики обработки ошибок, можно либо пробросить исключение дальше,
                // либо вернуть пустой список или null. Проброс предпочтительнее для оповещения о проблеме.
                throw;
            }
        }
        // >>> КОНЕЦ ДОБАВЛЕННОГО КОДА <<<

        // Вспомогательный метод сортировки (ИСПРАВЛЕНО)
        private IQueryable<Software> ApplySoftwareSorting(IQueryable<Software> query, string? sortBy, bool ascending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                sortBy = "Name"; // Сортировка по умолчанию
                ascending = true;
            }

            string sortByLower = sortBy.ToLowerInvariant();
            // keySelector гарантированно не будет null из-за default case в switch
            Expression<Func<Software, object>> keySelector = sortByLower switch
            {
                "name" => s => s.Name,
                "vendor" => s => (object)s.Vendor!,
                "version" => s => (object)s.Version!,
                "purchasedate" => s => (object?)s.PurchaseDate ?? DateTime.MaxValue,
                "expirydate" => s => (object?)s.ExpiryDate ?? DateTime.MaxValue,
                "seats" => s => (object?)s.Seats ?? int.MaxValue,
                "id" or "softwareid" => s => s.SoftwareId,
                _ => s => s.Name // Сортировка по умолчанию, если ключ не распознан
            };

            IOrderedQueryable<Software> orderedQuery;
            if (ascending)
            {
                orderedQuery = query.OrderBy(keySelector);
            }
            else
            {
                orderedQuery = query.OrderByDescending(keySelector);
            }
            // Дополнительная сортировка для стабильности
            return orderedQuery.ThenBy(s => s.SoftwareId);
        }

        public async Task<Software?> GetSoftwareByIdAsync(int softwareId)
        {
            SetupLogContext("GetSoftwareById", $"SoftwareId: {softwareId}");
            _logger.LogDebug("Запрос ПО по ID: {SoftwareId}", softwareId);
            try
            {
                var software = await _context.Software
                                           .FirstOrDefaultAsync(s => s.SoftwareId == softwareId);

                if (software == null)
                {
                    _logger.LogWarning("ПО с ID {SoftwareId} не найдено.", softwareId);
                }
                else
                {
                    _logger.LogInformation("ПО с ID {SoftwareId} ({SoftwareName}) успешно получено.", software.SoftwareId, software.Name);
                }
                return software;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении ПО по ID: {SoftwareId}", softwareId);
                throw;
            }
        }

        public async Task<Software> AddSoftwareAsync(Software software)
        {
            SetupLogContext("AddSoftware", $"Name: {software.Name}, Vendor: {software.Vendor}");
            var currentUser = _applicationStateService.CurrentUser;

            if (software == null) throw new ArgumentNullException(nameof(software));
            if (string.IsNullOrWhiteSpace(software.Name))
                throw new ArgumentException("Название ПО не может быть пустым.", nameof(software.Name));

            try
            {
                _context.Software.Add(software);
                await _context.SaveChangesAsync();

                _logger.LogInformation("ПО {SoftwareName} (ID: {SoftwareId}, Производитель: {Vendor}) успешно добавлено пользователем {CurrentUsername}.",
                    software.Name, software.SoftwareId, software.Vendor, currentUser?.Username ?? "System");
                return software;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении ПО {SoftwareName}.", software.Name);
                throw;
            }
        }

        public async Task<Software?> UpdateSoftwareAsync(Software software)
        {
            SetupLogContext("UpdateSoftware", $"SoftwareId: {software.SoftwareId}");
            var currentUser = _applicationStateService.CurrentUser;

            if (software == null) throw new ArgumentNullException(nameof(software));
            if (string.IsNullOrWhiteSpace(software.Name))
                throw new ArgumentException("Название ПО не может быть пустым.", nameof(software.Name));

            try
            {
                var existingSoftware = await _context.Software.FindAsync(software.SoftwareId);
                if (existingSoftware == null)
                {
                    _logger.LogWarning("ПО с ID {SoftwareId} не найдено для обновления.", software.SoftwareId);
                    return null;
                }

                existingSoftware.Name = software.Name;
                existingSoftware.Version = software.Version;
                existingSoftware.Vendor = software.Vendor;
                existingSoftware.LicenseKey = software.LicenseKey;
                existingSoftware.PurchaseDate = software.PurchaseDate;
                existingSoftware.ExpiryDate = software.ExpiryDate;
                existingSoftware.Seats = software.Seats;
                existingSoftware.Notes = software.Notes;

                await _context.SaveChangesAsync();
                _logger.LogInformation("ПО {SoftwareName} (ID: {SoftwareId}) успешно обновлено пользователем {CurrentUsername}.",
                    existingSoftware.Name, existingSoftware.SoftwareId, currentUser?.Username ?? "System");
                return existingSoftware;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Конфликт параллелизма при обновлении ПО ID: {SoftwareId}", software.SoftwareId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении ПО ID: {SoftwareId}", software.SoftwareId);
                throw;
            }
        }

        public async Task<bool> DeleteSoftwareAsync(int softwareId)
        {
            SetupLogContext("DeleteSoftware", $"SoftwareId: {softwareId}");
            var currentUser = _applicationStateService.CurrentUser;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var software = await _context.Software.FindAsync(softwareId);
                if (software != null)
                {
                    _context.Software.Remove(software);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _logger.LogInformation("ПО (ID: {SoftwareId}, Name: {SoftwareName}) успешно удалено пользователем {CurrentUsername}.",
                        softwareId, software.Name, currentUser?.Username ?? "System");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Попытка удалить несуществующее ПО с ID: {SoftwareId}", softwareId);
                    await transaction.RollbackAsync();
                    return false;
                }
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка базы данных при удалении ПО ID: {SoftwareId}. Возможно, ПО используется на устройствах.", softwareId);
                return false;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при удалении ПО ID: {SoftwareId}", softwareId);
                throw;
            }
        }
        // Метод GetSoftwareCountAsync, если он был в этом классе, остается без изменений
        public async Task<int> GetSoftwareCountAsync(SoftwareFilterParameters? filters)
        {
            SetupLogContext("GetSoftwareCount", filters != null ? $"Search: {filters.SearchTerm}, Vendor: {filters.Vendor}" : "No filters");
            _logger.LogDebug("Запрос общего количества ПО. Фильтры: {@Filters}", filters);
            try
            {
                var query = _context.Software.AsQueryable();
                if (filters != null)
                {
                    if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                    {
                        var term = filters.SearchTerm.ToLower();
                        query = query.Where(s =>
                            (s.Name.ToLower().Contains(term)) ||
                            (s.Version != null && s.Version.ToLower().Contains(term)) ||
                            (s.Vendor != null && s.Vendor.ToLower().Contains(term)) ||
                            (s.LicenseKey != null && s.LicenseKey.ToLower().Contains(term)) ||
                            (s.Notes != null && s.Notes.ToLower().Contains(term))
                        );
                    }
                    if (!string.IsNullOrWhiteSpace(filters.Vendor))
                    {
                        query = query.Where(s => s.Vendor != null && s.Vendor.ToLower().Contains(filters.Vendor.ToLower()));
                    }
                }
                var count = await query.CountAsync();
                _logger.LogInformation("Общее количество ПО по фильтрам: {SoftwareCount}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при подсчете ПО.");
                throw;
            }
        }
    }
}