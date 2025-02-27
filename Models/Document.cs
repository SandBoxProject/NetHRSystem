using System;
using System.ComponentModel.DataAnnotations;

namespace NetHRSystem.Models
{
    public class Document
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty; // Resume, ID, Contract, Certificate, etc.

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        [StringLength(100)]
        public string FileType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public bool IsPublic { get; set; } = false;

        [StringLength(200)]
        public string Tags { get; set; } = string.Empty;

        [StringLength(50)]
        public string Status { get; set; } = "Active";

        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        public DateTime UploadDate { get; set; } = DateTime.Now;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
