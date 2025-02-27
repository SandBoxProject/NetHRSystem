using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetHRSystem.Models
{
    public class Leave
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        [Required]
        public Guid LeaveTypeId { get; set; }
        public LeaveType LeaveType { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public Guid? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public Employee? ApprovedBy { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ApprovalDate { get; set; }

        [StringLength(500)]
        public string? Comments { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
