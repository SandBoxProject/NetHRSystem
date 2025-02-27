using System;
using System.ComponentModel.DataAnnotations;

namespace NetHRSystem.Models
{
    public class Setting
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string Type { get; set; } = "string"; // string, integer, decimal, boolean, date, json
        
        [Required]
        public string Category { get; set; } = "General";
        
        public bool IsReadOnly { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedAt { get; set; }
    }
}
