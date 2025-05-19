using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WpfApp1.Models;
using WpfApp1.Services.Data; // Для LogFilterParameters

namespace WpfApp1.Interfaces
{
    public interface ILogService
    {
        Task<IEnumerable<Log>> GetLogsAsync(LogFilterParameters filters, int pageNumber, int pageSize);
        Task<int> GetLogsCountAsync(LogFilterParameters filters);
        // Старый метод GetAllLogsAsync() можно удалить или пометить Obsolete
        // Task<IEnumerable<Log>> GetAllLogsAsync();
    }
}