using System;
using System.ComponentModel.DataAnnotations;

namespace NetHRSystem.Models
{
    public class Announcement
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        public Guid CreatedById { get; set; }
        public Employee CreatedBy { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
