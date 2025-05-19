// File: WpfApp1/Interfaces/IDeviceSoftwareService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WpfApp1.Models;

namespace WpfApp1.Interfaces
{
    public interface IDeviceSoftwareService
    {
        /// <summary>
        /// Получает список ПО, установленного на указанном устройстве, включая информацию о самом ПО.
        /// </summary>
        /// <param name="deviceId">Идентификатор устройства.</param>
        /// <returns>Коллекция записей DeviceSoftware с загруженными данными Software.</returns>
        Task<IEnumerable<DeviceSoftware>> GetSoftwareForDeviceAsync(int deviceId);

        /// <summary>
        /// Добавляет ПО к устройству.
        /// </summary>
        /// <param name="deviceId">Идентификатор устройства.</param>
        /// <param name="softwareId">Идентификатор ПО.</param>
        /// <param name="installedAt">Дата установки.</param>
        /// <returns>Созданная запись DeviceSoftware или null в случае ошибки.</returns>
        Task<DeviceSoftware?> AddSoftwareToDeviceAsync(int deviceId, int softwareId, DateTime installedAt);

        /// <summary>
        /// Удаляет связь ПО с устройством.
        /// </summary>
        /// <param name="deviceSoftwareId">Идентификатор записи DeviceSoftware (первичный ключ).</param>
        /// <returns>True, если удаление успешно, иначе false.</returns>
        Task<bool> RemoveSoftwareFromDeviceAsync(int deviceSoftwareId); // Используем ID из DeviceSoftware
    }
}