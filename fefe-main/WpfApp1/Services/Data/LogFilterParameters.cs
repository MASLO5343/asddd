using System;

namespace WpfApp1.Services.Data
{
    public class LogFilterParameters
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Level { get; set; }
        public string SearchText { get; set; } // Для поиска в Message
        public int? UserId { get; set; }       // Для фильтрации по UserId из кастомной колонки
        public string UserName { get; set; }   // Для фильтрации по UserName из кастомной колонки
        public string Action { get; set; }     // Для фильтрации по Action из кастомной колонки
    }
}