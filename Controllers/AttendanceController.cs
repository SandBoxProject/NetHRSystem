using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetHRSystem.Data;
using NetHRSystem.DTOs;
using NetHRSystem.Models;
using NetHRSystem.Services.UserService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetHRSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IUserService _userService;

        public AttendanceController(DataContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/Attendance
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetAllAttendance([FromQuery] DateTime? date)
        {
            var query = _context.Attendances
                .Include(a => a.Employee)
                .AsQueryable();

            if (date.HasValue)
            {
                var dateOnly = date.Value.Date;
                query = query.Where(a => a.Date.Date == dateOnly);
            }
            else
            {
                // Default to today if no date is provided
                var today = DateTime.Now.Date;
                query = query.Where(a => a.Date.Date == today);
            }

            var attendances = await query
                .Select(a => new AttendanceDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                    Date = a.Date,
                    TimeIn = a.TimeIn,
                    TimeOut = a.TimeOut,
                    IsPresent = a.IsPresent,
                    Status = a.Status,
                    WorkHours = a.WorkHours,
                    IsOvertime = a.IsOvertime,
                    OvertimeHours = a.OvertimeHours,
                    Notes = a.Notes
                })
                .ToListAsync();

            return Ok(attendances);
        }

        // GET: api/Attendance/MyAttendance
        [HttpGet("MyAttendance")]
        public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetMyAttendance([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null)
            {
                return BadRequest("Employee record not found for this user");
            }

            var query = _context.Attendances
                .Where(a => a.EmployeeId == employee.Id)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Date.Date >= startDate.Value.Date);
            }
            else
            {
                // Default to first day of current month if no start date is provided
                var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                query = query.Where(a => a.Date.Date >= firstDayOfMonth);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Date.Date <= endDate.Value.Date);
            }
            else
            {
                // Default to today if no end date is provided
                var today = DateTime.Now.Date;
                query = query.Where(a => a.Date.Date <= today);
            }

            var attendances = await query
                .OrderByDescending(a => a.Date)
                .Select(a => new AttendanceDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    Date = a.Date,
                    TimeIn = a.TimeIn,
                    TimeOut = a.TimeOut,
                    IsPresent = a.IsPresent,
                    Status = a.Status,
                    WorkHours = a.WorkHours,
                    IsOvertime = a.IsOvertime,
                    OvertimeHours = a.OvertimeHours,
                    Notes = a.Notes
                })
                .ToListAsync();

            return Ok(attendances);
        }

        // GET: api/Attendance/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AttendanceDto>> GetAttendance(Guid id)
        {
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attendance == null)
            {
                return NotFound();
            }

            // Check if the user is authorized to view this attendance
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);

            // Only allow if the attendance belongs to the employee or the user is an admin
            if (employee == null || (attendance.EmployeeId != employee.Id && !User.IsInRole("Admin")))
            {
                return Forbid();
            }

            var attendanceDto = new AttendanceDto
            {
                Id = attendance.Id,
                EmployeeId = attendance.EmployeeId,
                EmployeeName = $"{attendance.Employee.FirstName} {attendance.Employee.LastName}",
                Date = attendance.Date,
                TimeIn = attendance.TimeIn,
                TimeOut = attendance.TimeOut,
                IsPresent = attendance.IsPresent,
                Status = attendance.Status,
                WorkHours = attendance.WorkHours,
                IsOvertime = attendance.IsOvertime,
                OvertimeHours = attendance.OvertimeHours,
                Notes = attendance.Notes
            };

            return Ok(attendanceDto);
        }

        // POST: api/Attendance/ClockIn
        [HttpPost("ClockIn")]
        public async Task<ActionResult<Attendance>> ClockIn()
        {
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null)
            {
                return BadRequest("Employee record not found for this user");
            }

            var today = DateTime.Now.Date;
            var now = DateTime.Now;

            // Check if already clocked in today
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date.Date == today);

            if (existingAttendance != null)
            {
                if (existingAttendance.TimeIn.HasValue)
                {
                    return BadRequest("Already clocked in today");
                }

                // Update existing attendance record
                existingAttendance.TimeIn = now;
                existingAttendance.IsPresent = true;
                existingAttendance.Status = "Present";
                existingAttendance.UpdatedAt = now;

                await _context.SaveChangesAsync();
                return Ok(existingAttendance);
            }

            // Create new attendance record
            var attendance = new Attendance
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                Date = today,
                TimeIn = now,
                IsPresent = true,
                Status = "Present",
                CreatedAt = now
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAttendance), new { id = attendance.Id }, attendance);
        }

        // POST: api/Attendance/ClockOut
        [HttpPost("ClockOut")]
        public async Task<ActionResult<Attendance>> ClockOut()
        {
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null)
            {
                return BadRequest("Employee record not found for this user");
            }

            var today = DateTime.Now.Date;
            var now = DateTime.Now;

            // Check if already clocked in today
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.Date.Date == today);

            if (existingAttendance == null)
            {
                return BadRequest("Not clocked in today");
            }

            if (!existingAttendance.TimeIn.HasValue)
            {
                return BadRequest("Not clocked in today");
            }

            if (existingAttendance.TimeOut.HasValue)
            {
                return BadRequest("Already clocked out today");
            }

            // Update existing attendance record
            existingAttendance.TimeOut = now;
            existingAttendance.UpdatedAt = now;

            // Calculate work hours
            var timeIn = existingAttendance.TimeIn.Value;
            var workHours = (now - timeIn).TotalHours;
            existingAttendance.WorkHours = workHours;

            // Check if overtime (assuming 8 hours is standard)
            if (workHours > 8)
            {
                existingAttendance.IsOvertime = true;
                existingAttendance.OvertimeHours = workHours - 8;
            }

            await _context.SaveChangesAsync();
            return Ok(existingAttendance);
        }

        // POST: api/Attendance
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Attendance>> CreateAttendance(CreateAttendanceDto attendanceDto)
        {
            // Check if employee exists
            var employee = await _context.Employees.FindAsync(attendanceDto.EmployeeId);
            if (employee == null)
            {
                return BadRequest("Employee not found");
            }

            // Check if attendance record already exists for this employee and date
            var existingAttendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == attendanceDto.EmployeeId && a.Date.Date == attendanceDto.Date.Date);

            if (existingAttendance != null)
            {
                return BadRequest("Attendance record already exists for this employee and date");
            }

            // Calculate work hours if both time in and time out are provided
            double? workHours = null;
            bool isOvertime = false;
            double? overtimeHours = null;

            if (attendanceDto.TimeIn.HasValue && attendanceDto.TimeOut.HasValue)
            {
                workHours = (attendanceDto.TimeOut.Value - attendanceDto.TimeIn.Value).TotalHours;
                
                // Check if overtime (assuming 8 hours is standard)
                if (workHours > 8)
                {
                    isOvertime = true;
                    overtimeHours = workHours - 8;
                }
            }

            var attendance = new Attendance
            {
                Id = Guid.NewGuid(),
                EmployeeId = attendanceDto.EmployeeId,
                Date = attendanceDto.Date.Date,
                TimeIn = attendanceDto.TimeIn,
                TimeOut = attendanceDto.TimeOut,
                IsPresent = attendanceDto.IsPresent,
                Status = attendanceDto.Status,
                WorkHours = workHours,
                IsOvertime = isOvertime,
                OvertimeHours = overtimeHours,
                Notes = attendanceDto.Notes,
                CreatedAt = DateTime.Now
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAttendance), new { id = attendance.Id }, attendance);
        }

        // PUT: api/Attendance/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAttendance(Guid id, UpdateAttendanceDto attendanceDto)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
            {
                return NotFound();
            }

            // Calculate work hours if both time in and time out are provided
            double? workHours = null;
            bool isOvertime = false;
            double? overtimeHours = null;

            if (attendanceDto.TimeIn.HasValue && attendanceDto.TimeOut.HasValue)
            {
                workHours = (attendanceDto.TimeOut.Value - attendanceDto.TimeIn.Value).TotalHours;
                
                // Check if overtime (assuming 8 hours is standard)
                if (workHours > 8)
                {
                    isOvertime = true;
                    overtimeHours = workHours - 8;
                }
            }

            attendance.TimeIn = attendanceDto.TimeIn;
            attendance.TimeOut = attendanceDto.TimeOut;
            attendance.IsPresent = attendanceDto.IsPresent;
            attendance.Status = attendanceDto.Status;
            attendance.WorkHours = workHours;
            attendance.IsOvertime = isOvertime;
            attendance.OvertimeHours = overtimeHours;
            attendance.Notes = attendanceDto.Notes;
            attendance.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AttendanceExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Attendance/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAttendance(Guid id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
            {
                return NotFound();
            }

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Attendance/Summary
        [HttpGet("Summary")]
        public async Task<ActionResult<AttendanceSummaryDto>> GetAttendanceSummary([FromQuery] int? month, [FromQuery] int? year)
        {
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null)
            {
                return BadRequest("Employee record not found for this user");
            }

            // Default to current month and year if not provided
            var currentMonth = month ?? DateTime.Now.Month;
            var currentYear = year ?? DateTime.Now.Year;

            var startDate = new DateTime(currentYear, currentMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var attendances = await _context.Attendances
                .Where(a => a.EmployeeId == employee.Id && a.Date >= startDate && a.Date <= endDate)
                .ToListAsync();

            var workingDays = GetWorkingDaysInMonth(currentMonth, currentYear);
            var presentDays = attendances.Count(a => a.IsPresent);
            var absentDays = attendances.Count(a => !a.IsPresent);
            var lateDays = attendances.Count(a => a.Status == "Late");
            var totalWorkHours = attendances.Where(a => a.WorkHours.HasValue).Sum(a => a.WorkHours.Value);
            var totalOvertimeHours = attendances.Where(a => a.OvertimeHours.HasValue).Sum(a => a.OvertimeHours.Value);

            var summary = new AttendanceSummaryDto
            {
                Month = currentMonth,
                Year = currentYear,
                WorkingDays = workingDays,
                PresentDays = presentDays,
                AbsentDays = absentDays,
                LateDays = lateDays,
                AttendanceRate = workingDays > 0 ? (double)presentDays / workingDays * 100 : 0,
                TotalWorkHours = totalWorkHours,
                TotalOvertimeHours = totalOvertimeHours
            };

            return Ok(summary);
        }

        private bool AttendanceExists(Guid id)
        {
            return _context.Attendances.Any(e => e.Id == id);
        }

        private int GetWorkingDaysInMonth(int month, int year)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            int workingDays = 0;
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
            }

            return workingDays;
        }
    }
}
