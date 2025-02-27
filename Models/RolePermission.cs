using System;
using System.ComponentModel.DataAnnotations;

namespace NetHRSystem.Models
{
    public class RolePermission
    {
        [Key]
        public Guid Id { get; set; }

        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
