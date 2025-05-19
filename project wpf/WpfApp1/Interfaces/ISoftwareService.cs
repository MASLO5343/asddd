// File: WpfApp1/Interfaces/ISoftwareService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using WpfApp1.Models;

namespace WpfApp1.Interfaces
{
    public interface ISoftwareService
    {
        Task<IEnumerable<Software>> GetAllSoftwareAsync();
        Task<Software?> GetSoftwareByIdAsync(int softwareId);
        Task<Software> AddSoftwareAsync(Software software);
        Task<Software?> UpdateSoftwareAsync(Software software);
        Task<bool> DeleteSoftwareAsync(int softwareId);
    }
}