using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetHRSystem.Models
{
    public class LeaveType
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int DefaultDays { get; set; }

        [Required]
        public bool RequiresApproval { get; set; } = true;

        // Navigation properties
        public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
        public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    }
}
