// fefe-main/WpfApp1/Models/DeviceStatus.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    public class DeviceStatus
    {
        public DeviceStatus()
        {
            Devices = new HashSet<Device>();
        }

        [Key]
        public int StatusId { get; set; } // Или DeviceStatusId

        [Required]
        [StringLength(50)]
        public string StatusName { get; set; } = string.Empty;

        public int SortOrder { get; set; } // Для порядка отображения

        public virtual ICollection<Device> Devices { get; set; }
    }
}