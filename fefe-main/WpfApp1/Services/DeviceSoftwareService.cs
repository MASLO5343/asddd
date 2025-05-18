// fefe-main/WpfApp1/Services/DeviceSoftwareService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context; // Для LogContext
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WpfApp1.Interfaces;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class DeviceSoftwareService : IDeviceSoftwareService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DeviceSoftwareService> _logger;
        private readonly IApplicationStateService _applicationStateService; // ADDED
        // private readonly IIpAddressProvider _ipAddressProvider; // EXAMPLE

        public DeviceSoftwareService(
            AppDbContext context,
            ILogger<DeviceSoftwareService> logger,
            IApplicationStateService applicationStateService) // ADDED
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _applicationStateService = applicationStateService ?? throw new ArgumentNullException(nameof(applicationStateService)); // ADDED
        }

        private void SetupLogContext(string actionName, string? targetEntityInfo = null)
        {
            var currentUser = _applicationStateService.CurrentUser;
            LogContext.PushProperty("UserId", currentUser?.UserId);
            LogContext.PushProperty("UserName", currentUser?.Username);
            LogContext.PushProperty("Action", actionName);
            LogContext.PushProperty("TableAffected", "DeviceSoftware"); // Таблица связей
            // LogContext.PushProperty("IPAddress", _ipAddressProvider?.GetLocalIpAddress()); // EXAMPLE
        }

        public async Task<DeviceSoftware?> AddSoftwareToDeviceAsync(int deviceId, int softwareId, DateTime installedAt /* REMOVED: string notes */)
        {
            // CHANGED: targetEntityInfo to provide more context
            SetupLogContext("AddSoftwareToDevice", $"DeviceID: {deviceId}, SoftwareID: {softwareId}");
            var currentUser = _applicationStateService.CurrentUser;

            _logger.LogInformation("Attempting to add software ID: {SoftwareId} to device ID: {DeviceId} by user {CurrentUsername}",
                softwareId, deviceId, currentUser?.Username ?? "System");

            // Проверка на существование Device и Software (опционально, но рекомендуется)
            var deviceExists = await _context.Devices.AnyAsync(d => d.DeviceId == deviceId);
            if (!deviceExists)
            {
                _logger.LogWarning("Device with ID: {DeviceId} not found. Cannot add software.", deviceId);
                return null; // Или бросить исключение
            }
            var softwareExists = await _context.Software.AnyAsync(s => s.SoftwareId == softwareId);
            if (!softwareExists)
            {
                _logger.LogWarning("Software with ID: {SoftwareId} not found. Cannot add to device.", softwareId);
                return null; // Или бросить исключение
            }

            bool alreadyExists = await _context.DeviceSoftwares // CHANGED: Table name DeviceSoftwares
                                         .AnyAsync(ds => ds.DeviceId == deviceId && ds.SoftwareId == softwareId);

            if (alreadyExists)
            {
                _logger.LogWarning("Software ID: {SoftwareId} is already assigned to device ID: {DeviceId}", softwareId, deviceId);
                return null;
            }

            var newDeviceSoftware = new DeviceSoftware
            {
                DeviceId = deviceId,
                SoftwareId = softwareId,
                InstalledAt = installedAt // Используем обновленное имя поля
                // Notes = notes // REMOVED: Поле Notes удалено из модели DeviceSoftware
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.DeviceSoftwares.AddAsync(newDeviceSoftware); // CHANGED: Table name
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Successfully added software ID: {SoftwareId} to device ID: {DeviceId} by {CurrentUsername}. New DeviceSoftware Record ID: {DeviceSoftwareId}",
                                       softwareId, deviceId, currentUser?.Username ?? "System", newDeviceSoftware.Id);
                return newDeviceSoftware;
            }
            catch (DbUpdateException ex) // Более специфичная ошибка, если FK не существуют
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error adding software ID: {SoftwareId} to device ID: {DeviceId}. Ensure device and software exist.", softwareId, deviceId);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding software ID: {SoftwareId} to device ID: {DeviceId}", softwareId, deviceId);
                throw;
            }
        }

        public async Task<IEnumerable<DeviceSoftware>> GetSoftwareForDeviceAsync(int deviceId)
        {
            SetupLogContext("GetSoftwareForDevice", $"DeviceId: {deviceId}");
            _logger.LogInformation("Fetching software installations for device ID: {DeviceId}", deviceId);
            try
            {
                return await _context.DeviceSoftwares // CHANGED: Table name
                                     .Where(ds => ds.DeviceId == deviceId)
                                     .Include(ds => ds.Software)
                                     .OrderBy(ds => ds.Software != null ? ds.Software.Name : "")
                                     .ThenBy(ds => ds.InstalledAt)
                                     .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching software installations for device ID: {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task<IEnumerable<DeviceSoftware>> GetDevicesForSoftwareAsync(int softwareId) // New useful method
        {
            SetupLogContext("GetDevicesForSoftware", $"SoftwareId: {softwareId}");
            _logger.LogInformation("Fetching devices for software ID: {SoftwareId}", softwareId);
            try
            {
                return await _context.DeviceSoftwares // CHANGED: Table name
                                     .Where(ds => ds.SoftwareId == softwareId)
                                     .Include(ds => ds.Device)
                                     .OrderBy(ds => ds.Device != null ? ds.Device.Name : "")
                                     .ThenBy(ds => ds.InstalledAt)
                                     .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching devices for software ID: {SoftwareId}", softwareId);
                throw;
            }
        }


        public async Task<bool> RemoveSoftwareFromDeviceAsync(int deviceSoftwareId)
        {
            SetupLogContext("RemoveSoftwareFromDevice", $"DeviceSoftwareId: {deviceSoftwareId}");
            var currentUser = _applicationStateService.CurrentUser;
            _logger.LogInformation("Attempting to remove DeviceSoftware record with ID: {DeviceSoftwareId} by user {CurrentUsername}",
                deviceSoftwareId, currentUser?.Username ?? "System");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var deviceSoftware = await _context.DeviceSoftwares.FindAsync(deviceSoftwareId); // CHANGED: Table name

                if (deviceSoftware == null)
                {
                    _logger.LogWarning("DeviceSoftware record with ID: {DeviceSoftwareId} not found for deletion.", deviceSoftwareId);
                    await transaction.RollbackAsync();
                    return false;
                }

                _context.DeviceSoftwares.Remove(deviceSoftware); // CHANGED: Table name
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("DeviceSoftware record with ID: {DeviceSoftwareId} (DeviceID: {DeviceId}, SoftwareID: {SoftwareId}) removed successfully by {CurrentUsername}.",
                    deviceSoftwareId, deviceSoftware.DeviceId, deviceSoftware.SoftwareId, currentUser?.Username ?? "System");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error removing DeviceSoftware record with ID: {DeviceSoftwareId}", deviceSoftwareId);
                throw;
            }
        }
    }
}