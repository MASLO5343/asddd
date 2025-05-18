// fefe-main/WpfApp1/Constants/Permissions.cs
namespace WpfApp1.Constants
{
    public static class Permissions
    {
        // Права для Пользователей
        public const string ViewUsers = "Users.View";
        public const string ManageUsers = "Users.Manage"; // Создание, редактирование, активация/деактивация

        // Права для Инвентаризации
        public const string ViewInventory = "Inventory.View"; // Сохранено из первого фрагмента, см. также Pages.InventoryView
        public const string ManageInventory = "Inventory.Manage"; // Добавление, редактирование, удаление

        // Права для Заявок
        public const string ViewTickets = "Tickets.View"; // Сохранено из первого фрагмента, см. также Pages.TicketsView
        public const string ManageTickets = "Tickets.Manage"; // Создание, редактирование, смена статуса
        public const string AssignTickets = "Tickets.Assign"; // Назначение исполнителя
        public const string CommentTickets = "Tickets.Comment"; // Добавление комментариев

        // Права для Администрирования
        public const string AccessAdminSection = "Admin.Access"; // Доступ к разделу администрирования

        // Права для Мониторинга
        public const string ViewMonitoring = "Monitoring.View"; // Право просмотра раздела Мониторинг, см. также Pages.MonitoringView

        // Права для Настроек
        public const string ManageSettings = "Settings.Manage"; // Право управления настройками

        // Разрешения для логирования/просмотра событий
        // Используется значение из первого фрагмента, которое было помечено как "Новое значение из второго фрагмента"
        // Во втором фрагменте Pages.LogsView также ссылается на "Permissions.Logs.View", что может потребовать унификации именования.
        // Если Pages.LogsView должно иметь значение "Pages.Logs.View", а это разрешение для операций, то они разные.
        // Если же это одно и то же право, то следует выбрать одно каноническое имя.
        // Примем, что "Permissions.Logs.View" - это право на действие, а "Pages.Logs.View" - на просмотр страницы логов.
        public const string ViewSystemLogs = "Permissions.Logs.View"; // Переименовано для ясности отличения от Pages.LogsView
        // public const string ManageLogs = "Permissions.Logs.Manage"; // Если будет функционал управления логами (Закомментировано, как и ранее)

        // Статический вложенный класс для разрешений на просмотр страниц
        public static class Pages
        {
            public const string DashboardView = "Pages.Dashboard.View";
            public const string InventoryView = "Pages.Inventory.View"; // Соответствует общему праву ViewInventory, но специфично для страницы
            public const string UsersView = "Pages.Users.View";         // Соответствует общему праву ViewUsers, но специфично для страницы
            public const string TicketsView = "Pages.Tickets.View";       // Соответствует общему праву ViewTickets, но специфично для страницы
            public const string LogsView = "Pages.Logs.View";           // Право на просмотр страницы логов
            public const string MonitoringView = "Pages.Monitoring.View"; // Соответствует общему праву ViewMonitoring, но специфично для страницы
            // Добавьте другие страницы по аналогии
        }

        // Другие права по мере необходимости...
        // Например, если потребуется гранулярное управление для раздела администрирования:
        // public const string ManageSystemParameters = "Admin.SystemParameters.Manage";
        // public const string ViewAuditTrail = "Admin.AuditTrail.View";
    }
}