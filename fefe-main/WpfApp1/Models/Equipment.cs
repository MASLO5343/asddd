namespace WpfApp1.Models
{
    public class Equipment
    {
        public int Id { get; set; }

        // Название оборудования (например, "Сервер 1", "Принтер HP")
        public string Name { get; set; } = string.Empty;

        // Тип оборудования (например, "Сервер", "ПК", "Принтер", "Маршрутизатор")
        public string Type { get; set; } = string.Empty;

        // Местоположение оборудования (например, "ЦОД", "Офис 101")
        public string Location { get; set; } = string.Empty;

        // Дополнительные свойства, например, серийный номер или модель:
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
    }
}