using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetHRSystem.Models
{
    public class Claim
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        public Guid ClaimTypeId { get; set; }
        public ClaimType ClaimType { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ClaimDate { get; set; }

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

        [StringLength(500)]
        public string? ReceiptUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
