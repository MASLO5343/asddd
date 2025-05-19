using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1.Pages
{
    public partial class ColumnSettingsPage : Page
    {
        public List<ColumnVisibilitySetting> ColumnSettings { get; set; }

        public ColumnSettingsPage()
        {
            InitializeComponent();
            var settings = Application.Current.Properties["DeviceColumnSettings"] as List<ColumnVisibilitySetting>;
            if (settings == null)
            {
                LoadDefaultSettings();
            }
            else
            {
                ColumnSettings = settings;
            }

            DataContext = this;
        }

        private void LoadDefaultSettings()
        {
            // Можно расширить загрузку из файла/БД позже
            ColumnSettings = new List<ColumnVisibilitySetting>
            {
                new("DeviceId", "Идентификатор", false),
                new("InventoryNumber", "Инвентарный номер", true),
                new("Name", "Название устройства", true),
                new("TypeId", "Тип", true),
                new("IPAddress", "IP-адрес", true),
                new("MACAddress", "MAC-адрес", false),
                new("Location", "Местоположение", false),
                new("Department", "Отдел", false),
                new("ResponsibleUserId", "Ответственный пользователь", false),
                new("StatusId", "Текущий статус", false),
                new("OS", "Операционная система", false),
                new("PurchaseDate", "Дата приобретения", false),
                new("WarrantyUntil", "Гарантия до", false),
                new("Notes", "Примечание", false)
            };
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Properties["DeviceColumnSettings"] = ColumnSettings;
        }
    }

    public class ColumnVisibilitySetting
    {
        public string PropertyName { get; set; }
        public string DisplayName { get; set; }
        public bool ShowInMainGrid { get; set; }

        public ColumnVisibilitySetting(string prop, string display, bool show)
        {
            PropertyName = prop;
            DisplayName = display;
            ShowInMainGrid = show;
        }
    }
}

