// File: WpfApp1/Services/Data/TicketFilterParameters.cs 
// (Создайте папку Data внутри Services, если её нет, или используйте другое подходящее место)
using System;

namespace WpfApp1.Services.Data
{
    /// <summary>
    /// Параметры для фильтрации списка заявок.
    /// </summary>
    public class TicketFilterParameters
    {
        /// <summary>
        /// Текст для поиска по теме или описанию заявки.
        /// </summary>
        public string? SearchText { get; set; }

        /// <summary>
        /// Фильтр по идентификатору статуса.
        /// </summary>
        public int? StatusId { get; set; }

        /// <summary>
        /// Фильтр по идентификатору приоритета.
        /// </summary>
        public int? PriorityId { get; set; }

        /// <summary>
        /// Фильтр по идентификатору назначенного исполнителя.
        /// </summary>
        public int? AssignedToUserId { get; set; }

        /// <summary>
        /// Фильтр по идентификатору создателя заявки.
        /// </summary>
        public int? CreatedByUserId { get; set; }

        /// <summary>
        /// Фильтр по идентификатору связанного устройства.
        /// </summary>
        public int? DeviceId { get; set; }

        /// <summary>
        /// Фильтр по дате создания заявки (начало периода).
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Фильтр по дате создания заявки (конец периода).
        /// </summary>
        public DateTime? DateTo { get; set; }
    }
}