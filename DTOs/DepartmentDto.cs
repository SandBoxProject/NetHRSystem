using System;
using System.Collections.Generic;

namespace NetHRSystem.DTOs
{
    public class DepartmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? ManagerId { get; set; }
        public int EmployeeCount { get; set; }
    }

    public class DepartmentDetailDto : DepartmentDto
    {
        public string? ManagerName { get; set; }
        public List<EmployeeDto> Employees { get; set; } = new List<EmployeeDto>();
    }

    public class CreateDepartmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? ManagerId { get; set; }
    }

    public class UpdateDepartmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? ManagerId { get; set; }
    }
}
