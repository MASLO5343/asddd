// fefe-main/WpfApp1/Services/DeviceService.cs
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
using WpfApp1.Services.Data;

namespace WpfApp1.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DeviceService> _logger;
        private readonly IApplicationStateService _applicationStateService;

        public DeviceService(AppDbContext context, ILogger<DeviceService> logger, IApplicationStateService applicationStateService)
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
            LogContext.PushProperty("TableAffected", "Devices");
        }

        public async Task<IEnumerable<Device>> GetDevicesAsync(DeviceFilterParameters? filters, string? sortBy = null, bool ascending = true, int pageNumber = 1, int pageSize = 20)
        {
            SetupLogContext("GetDevicesFiltered", filters != null ? $"Search: {filters.SearchTerm}, StatusId: {filters.StatusId}" : "No filters");
            _logger.LogDebug("Запрос списка устройств. Фильтры: {@Filters}, Сортировка: {SortBy} {Ascending}, Стр: {Page}, Размер: {PageSize}", filters, sortBy, ascending, pageNumber, pageSize);
            try
            {
                var query = _context.Devices
                                    .Include(d => d.DeviceType)
                                    .Include(d => d.ResponsibleUser)
                                    .Include(d => d.DeviceStatus)
                                    .AsQueryable();

                if (filters != null)
                {
                    if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                    {
                        var term = filters.SearchTerm.ToLower();
                        query = query.Where(d =>
                            (d.InventoryNumber != null && d.InventoryNumber.ToLower().Contains(term)) ||
                            (d.Name.ToLower().Contains(term)) ||
                            (d.IpAddress != null && d.IpAddress.ToLower().Contains(term)) ||
                            (d.MacAddress != null && d.MacAddress.ToLower().Contains(term)) ||
                            (d.Location != null && d.Location.ToLower().Contains(term)) ||
                            (d.Department != null && d.Department.ToLower().Contains(term)) ||
                            (d.Notes != null && d.Notes.ToLower().Contains(term))
                        );
                    }
                    if (filters.TypeId.HasValue)
                        query = query.Where(d => d.DeviceTypeId == filters.TypeId.Value);

                    if (!string.IsNullOrWhiteSpace(filters.Department))
                        query = query.Where(d => d.Department != null && d.Department.ToLower().Contains(filters.Department.ToLower()));

                    if (filters.ResponsibleUserId.HasValue)
                        query = query.Where(d => d.ResponsibleUserId == filters.ResponsibleUserId.Value);

                    if (filters.StatusId.HasValue)
                        query = query.Where(d => d.StatusId == filters.StatusId.Value);

                    if (!string.IsNullOrWhiteSpace(filters.Location))
                        query = query.Where(d => d.Location != null && d.Location.ToLower().Contains(filters.Location.ToLower()));
                }

                query = ApplyDeviceSorting(query, sortBy, ascending);

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var devices = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
                _logger.LogInformation("Успешно получено {DeviceCount} устройств.", devices.Count);
                return devices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка устройств.");
                throw;
            }
        }

        public async Task<int> GetDevicesCountAsync(DeviceFilterParameters? filters)
        {
            SetupLogContext("GetDevicesCount", filters != null ? $"Search: {filters.SearchTerm}" : "No filters");
            _logger.LogDebug("Запрос общего количества устройств. Фильтры: {@Filters}", filters);
            try
            {
                var query = _context.Devices.AsQueryable();
                if (filters != null)
                {
                    if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                    {
                        var term = filters.SearchTerm.ToLower();
                        query = query.Where(d =>
                            (d.InventoryNumber != null && d.InventoryNumber.ToLower().Contains(term)) ||
                            (d.Name.ToLower().Contains(term)) ||
                            (d.IpAddress != null && d.IpAddress.ToLower().Contains(term)) ||
                            (d.MacAddress != null && d.MacAddress.ToLower().Contains(term)) ||
                            (d.Location != null && d.Location.ToLower().Contains(term)) ||
                            (d.Department != null && d.Department.ToLower().Contains(term)) ||
                            (d.Notes != null && d.Notes.ToLower().Contains(term))
                        );
                    }
                    if (filters.TypeId.HasValue)
                        query = query.Where(d => d.DeviceTypeId == filters.TypeId.Value);
                    if (!string.IsNullOrWhiteSpace(filters.Department))
                        query = query.Where(d => d.Department != null && d.Department.ToLower().Contains(filters.Department.ToLower()));
                    if (filters.ResponsibleUserId.HasValue)
                        query = query.Where(d => d.ResponsibleUserId == filters.ResponsibleUserId.Value);
                    if (filters.StatusId.HasValue)
                        query = query.Where(d => d.StatusId == filters.StatusId.Value);
                    if (!string.IsNullOrWhiteSpace(filters.Location))
                        query = query.Where(d => d.Location != null && d.Location.ToLower().Contains(filters.Location.ToLower()));
                }
                var count = await query.CountAsync();
                _logger.LogInformation("Общее количество устройств по фильтрам: {DeviceCount}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при подсчете устройств.");
                throw;
            }
        }

        private IQueryable<Device> ApplyDeviceSorting(IQueryable<Device> query, string? sortBy, bool ascending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
            {
                sortBy = "Name";
                ascending = true;
            }

            string sortByLower = sortBy.ToLowerInvariant();
            Expression<Func<Device, object>>? keySelector = sortByLower switch
            {
                "name" => d => d.Name,
                "inventorynumber" => d => (object)d.InventoryNumber!,
                "ipaddress" => d => (object)d.IpAddress!,
                "devicetype" or "devicetype.name" => d => d.DeviceType != null ? (object)d.DeviceType.Name : "",
                "status" or "devicestatus.statusname" => d => d.DeviceStatus != null ? (object)d.DeviceStatus.StatusName : "",
                "location" => d => (object)d.Location!,
                "department" => d => (object)d.Department!,
                "responsibleuser" or "responsibleuser.fullname" => d => d.ResponsibleUser != null ? (object)d.ResponsibleUser.FullName! : "",
                "warrantyuntil" => d => (object?)d.WarrantyUntil ?? DateOnly.MaxValue,
                "deviceid" or "id" => d => d.DeviceId,
                _ => d => d.Name
            };

            if (keySelector != null)
            {
                if (ascending)
                {
                    query = query.OrderBy(keySelector).ThenBy(d => d.DeviceId);
                }
                else
                {
                    query = query.OrderByDescending(keySelector).ThenByDescending(d => d.DeviceId);
                }
            }
            return query;
        }

        public async Task<Device?> GetDeviceByIdAsync(int deviceId)
        {
            SetupLogContext("GetDeviceById", $"DeviceId: {deviceId}");
            _logger.LogDebug("Запрос устройства по ID: {DeviceId}", deviceId);
            try
            {
                var device = await _context.Devices
                                        .Include(d => d.DeviceType)
                                        .Include(d => d.ResponsibleUser)
                                        .Include(d => d.DeviceStatus)
                                        .Include(d => d.DeviceSoftwares!)
                                            .ThenInclude(ds => ds.Software)
                                        .FirstOrDefaultAsync(d => d.DeviceId == deviceId);
                if (device == null)
                {
                    _logger.LogWarning("Устройство с ID {DeviceId} не найдено.", deviceId);
                }
                else
                {
                    _logger.LogInformation("Устройство с ID {DeviceId} ({DeviceName}) успешно получено.", device.DeviceId, device.Name);
                }
                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении устройства по ID: {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task AddDeviceAsync(Device device)
        {
            SetupLogContext("AddDevice", $"Name: {device.Name}, InvNo: {device.InventoryNumber}");
            var currentUser = _applicationStateService.CurrentUser;

            if (device == null) throw new ArgumentNullException(nameof(device));
            if (string.IsNullOrWhiteSpace(device.Name))
                throw new ArgumentException("Имя устройства не может быть пустым.", nameof(device.Name));
            if (device.DeviceTypeId == null || device.DeviceTypeId == 0)
                throw new ArgumentException("Тип устройства должен быть указан.", nameof(device.DeviceTypeId));
            if (device.StatusId == 0)
                throw new ArgumentException("Статус устройства должен быть указан.", nameof(device.StatusId));

            if (!string.IsNullOrWhiteSpace(device.InventoryNumber) &&
                await _context.Devices.AnyAsync(d => d.InventoryNumber == device.InventoryNumber))
            {
                _logger.LogWarning("Попытка добавить устройство с уже существующим инвентарным номером: {InventoryNumber}", device.InventoryNumber);
                throw new ArgumentException("Устройство с таким инвентарным номером уже существует.");
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Devices.Add(device);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Устройство {DeviceName} (ID: {DeviceId}, Инв.№: {InventoryNumber}) успешно добавлено пользователем {CurrentUsername}.",
                    device.Name, device.DeviceId, device.InventoryNumber, currentUser?.Username ?? "System");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при добавлении устройства {DeviceName}.", device.Name);
                throw;
            }
        }

        public async Task UpdateDeviceAsync(Device device)
        {
            SetupLogContext("UpdateDevice", $"DeviceId: {device.DeviceId}");
            var currentUser = _applicationStateService.CurrentUser;

            if (device == null) throw new ArgumentNullException(nameof(device));
            if (string.IsNullOrWhiteSpace(device.Name))
                throw new ArgumentException("Имя устройства не может быть пустым.", nameof(device.Name));
            if (device.DeviceTypeId == null || device.DeviceTypeId == 0)
                throw new ArgumentException("Тип устройства должен быть указан.", nameof(device.DeviceTypeId));
            if (device.StatusId == 0)
                throw new ArgumentException("Статус устройства должен быть указан.", nameof(device.StatusId));

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingDevice = await _context.Devices.FindAsync(device.DeviceId);
                if (existingDevice == null)
                {
                    _logger.LogError("Попытка обновить несуществующее устройство с ID: {DeviceId}", device.DeviceId);
                    await transaction.RollbackAsync();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(device.InventoryNumber) &&
                    existingDevice.InventoryNumber != device.InventoryNumber &&
                    await _context.Devices.AnyAsync(d => d.InventoryNumber == device.InventoryNumber && d.DeviceId != device.DeviceId))
                {
                    _logger.LogWarning("Попытка изменить инвентарный номер на уже существующий: {InventoryNumber}", device.InventoryNumber);
                    await transaction.RollbackAsync();
                    throw new ArgumentException("Устройство с таким инвентарным номером уже существует.");
                }

                existingDevice.Name = device.Name;
                existingDevice.InventoryNumber = device.InventoryNumber;
                existingDevice.IpAddress = device.IpAddress;
                existingDevice.MacAddress = device.MacAddress;
                existingDevice.Location = device.Location;
                existingDevice.Department = device.Department;
                existingDevice.Notes = device.Notes;
                existingDevice.WarrantyUntil = device.WarrantyUntil;
                existingDevice.DeviceTypeId = device.DeviceTypeId;
                existingDevice.StatusId = device.StatusId;
                existingDevice.ResponsibleUserId = device.ResponsibleUserId;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Устройство {DeviceName} (ID: {DeviceId}) успешно обновлено пользователем {CurrentUsername}.",
                    existingDevice.Name, existingDevice.DeviceId, currentUser?.Username ?? "System");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Конфликт параллелизма при обновлении устройства ID: {DeviceId}", device.DeviceId);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при обновлении устройства ID: {DeviceId}", device.DeviceId);
                throw;
            }
        }

        public async Task DeleteDeviceAsync(int deviceId)
        {
            SetupLogContext("DeleteDevice", $"DeviceId: {deviceId}");
            var currentUser = _applicationStateService.CurrentUser;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var device = await _context.Devices.FindAsync(deviceId);
                if (device != null)
                {
                    _context.Devices.Remove(device);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _logger.LogInformation("Устройство (ID: {DeviceId}) успешно удалено пользователем {CurrentUsername}.",
                        deviceId, currentUser?.Username ?? "System");
                }
                else
                {
                    _logger.LogWarning("Попытка удалить несуществующее устройство с ID: {DeviceId}", deviceId);
                    await transaction.RollbackAsync();
                }
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка базы данных при удалении устройства ID: {DeviceId}. Возможны активные связи, не настроенные на каскадное удаление.", deviceId);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при удалении устройства ID: {DeviceId}", deviceId);
                throw;
            }
        }
        public async Task<IEnumerable<DeviceType>> GetAllDeviceTypesAsync()
        {
            SetupLogContext("GetAllDeviceTypes");
            _logger.LogInformation("Получение всех типов устройств.");
            try
            {
                return await _context.DeviceTypes.OrderBy(dt => dt.Name).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении типов устройств.");
                throw;
            }
        }

        public async Task<IEnumerable<DeviceStatus>> GetAllDeviceStatusesAsync()
        {
            SetupLogContext("GetAllDeviceStatuses");
            _logger.LogInformation("Получение всех статусов устройств.");
            try
            {
                return await _context.DeviceStatuses.OrderBy(ds => ds.SortOrder).ThenBy(ds => ds.StatusName).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статусов устройств.");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetAvailableDepartmentsAsync()
        {
            SetupLogContext("GetAvailableDepartments");
            _logger.LogInformation("Получение доступных отделов.");
            try
            {
                return await _context.Devices
                    .Where(d => d.Department != null && d.Department != "")
                    .Select(d => d.Department!)
                    .Distinct()
                    .OrderBy(dep => dep)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка отделов.");
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetAvailableResponsibleUsersAsync()
        {
            SetupLogContext("GetAvailableResponsibleUsers");
            _logger.LogInformation("Получение доступных ответственных пользователей.");
            try
            {
                // Возвращаем всех активных пользователей, которые могут быть ответственными
                // Можно добавить фильтр, если только определенные роли могут быть ответственными
                return await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка ответственных пользователей.");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetAvailableLocationsAsync()
        {
            SetupLogContext("GetAvailableLocations");
            _logger.LogInformation("Получение доступных местоположений.");
            try
            {
                return await _context.Devices
                    .Where(d => d.Location != null && d.Location != "")
                    .Select(d => d.Location!)
                    .Distinct()
                    .OrderBy(loc => loc)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка местоположений.");
                throw;
            }
        }

        // ----- Stubs for other IDeviceService methods if they are not being moved -----
        public async Task AddSoftwareToDeviceAsync(int deviceId, int softwareId, DateTime installDate, string notes)
        {
            _logger.LogWarning($"Attempted to call {nameof(AddSoftwareToDeviceAsync)} which is intended to be in a different service. DeviceId: {deviceId}, SoftwareId: {softwareId}");
            await Task.CompletedTask;
            throw new NotImplementedException($"{nameof(AddSoftwareToDeviceAsync)} was moved to DeviceSoftwareService. Update IDeviceService or implement if still required here.");
        }

        public async Task RemoveSoftwareFromDeviceAsync(int deviceId, int softwareId)
        {
            _logger.LogWarning($"Attempted to call {nameof(RemoveSoftwareFromDeviceAsync)} which is intended to be in a different service. DeviceId: {deviceId}, SoftwareId: {softwareId}");
            await Task.CompletedTask;
            throw new NotImplementedException($"{nameof(RemoveSoftwareFromDeviceAsync)} was moved to DeviceSoftwareService. Update IDeviceService or implement if still required here.");
        }

        public async Task<IEnumerable<Software>> GetSoftwareForDeviceAsync(int deviceId)
        {
            _logger.LogWarning($"Attempted to call {nameof(GetSoftwareForDeviceAsync)} which is intended to be in a different service. DeviceId: {deviceId}");
            await Task.CompletedTask;
            throw new NotImplementedException($"{nameof(GetSoftwareForDeviceAsync)} was moved to DeviceSoftwareService. Update IDeviceService or implement if still required here.");
        }

        public async Task<IEnumerable<Software>> GetAllSoftwareAsync()
        {
            _logger.LogWarning($"Attempted to call {nameof(GetAllSoftwareAsync)} which is intended to be in a different service (SoftwareService).");
            await Task.CompletedTask;
            throw new NotImplementedException($"{nameof(GetAllSoftwareAsync)} was moved to SoftwareService. Update IDeviceService or implement if still required here.");
        }
    }
}