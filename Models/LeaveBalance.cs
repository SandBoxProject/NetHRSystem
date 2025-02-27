using System;
using System.ComponentModel.DataAnnotations;

namespace NetHRSystem.Models
{
    public class LeaveBalance
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public Guid LeaveTypeId { get; set; }
        public LeaveType LeaveType { get; set; } = null!;

        [Required]
        public int AllottedDays { get; set; }

        [Required]
        public int UsedDays { get; set; }

        public int RemainingDays => AllottedDays - UsedDays;

        [Required]
        public int Year { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
