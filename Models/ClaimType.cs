using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetHRSystem.Models
{
    public class ClaimType
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaximumAmount { get; set; }

        [Required]
        public bool RequiresReceipt { get; set; } = true;

        [Required]
        public bool RequiresApproval { get; set; } = true;

        // Navigation properties
        public ICollection<Claim> Claims { get; set; } = new List<Claim>();
    }
}
