using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WpfApp1.Models
{
    public partial class Role
    {
        public Role()
        {
            Users = new HashSet<User>();
        }

        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = null!;

        [StringLength(255)]
        public string? Description { get; set; }

        // Навигационное свойство-коллекция
        public virtual ICollection<User> Users { get; set; }
    }
}