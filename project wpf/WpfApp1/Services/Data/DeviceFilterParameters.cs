namespace WpfApp1.Services.Data
{
    public class DeviceFilterParameters
    {
        public string SearchTerm { get; set; } // Общий поиск по нескольким полям
        public int? TypeId { get; set; }
        public string Department { get; set; }
        public int? ResponsibleUserId { get; set; }
        public int? StatusId { get; set; }
        public string Location { get; set; }
        public string OS { get; set; }
    }
}