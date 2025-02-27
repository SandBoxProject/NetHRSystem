using System;

namespace NetHRSystem.DTOs
{
    public class LeaveDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public Guid LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateLeaveDto
    {
        public Guid LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ApproveLeaveDto
    {
        public bool Approved { get; set; }
        public string? Comments { get; set; }
    }

    public class LeaveTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DefaultDays { get; set; }
        public bool RequiresApproval { get; set; }
    }

    public class LeaveBalanceDto
    {
        public Guid? Id { get; set; }
        public Guid LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public int AllottedDays { get; set; }
        public int UsedDays { get; set; }
        public int RemainingDays { get; set; }
        public int Year { get; set; }
    }
}
