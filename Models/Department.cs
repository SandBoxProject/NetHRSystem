using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NetHRSystem.Models
{
    public class Department
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public Guid? ManagerId { get; set; }

        // Navigation properties
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
