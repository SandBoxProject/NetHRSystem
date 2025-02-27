using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NetHRSystem.Models
{
    public class Employee
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; }

        [StringLength(100)]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [StringLength(20)]
        public string ZipCode { get; set; } = string.Empty;

        [StringLength(50)]
        public string Country { get; set; } = string.Empty;

        [StringLength(50)]
        public string JobTitle { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Salary { get; set; }

        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public Guid? ManagerId { get; set; }
        [ForeignKey("ManagerId")]
        public Employee? Manager { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        // Navigation properties
        public ICollection<Leave> Leaves { get; set; } = new List<Leave>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<Claim> Claims { get; set; } = new List<Claim>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
