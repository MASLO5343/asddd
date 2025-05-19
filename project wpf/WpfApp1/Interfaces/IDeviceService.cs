using System; // Added for DateTime
using System.Collections.Generic;
using System.Threading.Tasks;
using WpfApp1.Models;
using WpfApp1.Services.Data; // Для DeviceFilterParameters

namespace WpfApp1.Interfaces
{
    public interface IDeviceService
    {
        Task<IEnumerable<Device>> GetDevicesAsync(DeviceFilterParameters filters, string sortBy = null, bool ascending = true, int pageNumber = 1, int pageSize = 100);
        Task<int> GetDevicesCountAsync(DeviceFilterParameters filters);
        Task<Device> GetDeviceByIdAsync(int deviceId);
        Task AddDeviceAsync(Device device);
        Task UpdateDeviceAsync(Device device);
        Task DeleteDeviceAsync(int deviceId);
        Task<IEnumerable<DeviceType>> GetAllDeviceTypesAsync();
        Task<IEnumerable<DeviceStatus>> GetAllDeviceStatusesAsync();

        // Methods for fetching distinct filter values
        Task<IEnumerable<string>> GetAvailableDepartmentsAsync();
        Task<IEnumerable<User>> GetAvailableResponsibleUsersAsync(); // Assuming User model is appropriate here
        Task<IEnumerable<string>> GetAvailableLocationsAsync();


        // Keep existing software-related methods as per your interface,
        // but note they were marked as potentially moved in DeviceService.cs comments.
        // If these are indeed handled by DeviceSoftwareService or SoftwareService,
        // this interface and its implementations might need further refactoring.
        Task AddSoftwareToDeviceAsync(int deviceId, int softwareId, DateTime installDate, string notes);
        Task RemoveSoftwareFromDeviceAsync(int deviceId, int softwareId);
        Task<IEnumerable<Software>> GetSoftwareForDeviceAsync(int deviceId);
        Task<IEnumerable<Software>> GetAllSoftwareAsync();
    }
}