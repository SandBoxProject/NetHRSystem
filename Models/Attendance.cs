using System;
using System.ComponentModel.DataAnnotations;

namespace NetHRSystem.Models
{
    public class Attendance
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [DataType(DataType.Time)]
        public DateTime? TimeIn { get; set; }

        [DataType(DataType.Time)]
        public DateTime? TimeOut { get; set; }

        public bool IsPresent { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Present"; // Present, Absent, Late, Half-Day

        public double? WorkHours { get; set; }

        public bool IsOvertime { get; set; }

        public double? OvertimeHours { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
