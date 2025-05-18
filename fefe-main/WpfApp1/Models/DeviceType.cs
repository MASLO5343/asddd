using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    public partial class DeviceType // Имя файла DeviceType.cs
    {
        public DeviceType()
        {
            Devices = new HashSet<Device>();
        }

        [Key]
        public int DeviceTypeId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        // Другие свойства, если есть (например, Description)

        public virtual ICollection<Device> Devices { get; set; }
    }
}