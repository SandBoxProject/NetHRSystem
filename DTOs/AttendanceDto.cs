using System;

namespace NetHRSystem.DTOs
{
    public class AttendanceDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public bool IsPresent { get; set; }
        public string Status { get; set; } = string.Empty;
        public double? WorkHours { get; set; }
        public bool IsOvertime { get; set; }
        public double? OvertimeHours { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateAttendanceDto
    {
        public Guid EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public bool IsPresent { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class UpdateAttendanceDto
    {
        public DateTime? TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public bool IsPresent { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class AttendanceSummaryDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int WorkingDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public double AttendanceRate { get; set; }
        public double TotalWorkHours { get; set; }
        public double TotalOvertimeHours { get; set; }
    }
}
