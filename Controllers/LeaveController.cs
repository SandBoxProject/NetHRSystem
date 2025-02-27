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
    public class LeaveController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IUserService _userService;

        public LeaveController(DataContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/Leave
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<LeaveDto>>> GetAllLeaves()
        {
            var leaves = await _context.Leaves
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .Include(l => l.ApprovedBy)
                .Select(l => new LeaveDto
                {
                    Id = l.Id,
                    EmployeeId = l.EmployeeId,
                    EmployeeName = $"{l.Employee.FirstName} {l.Employee.LastName}",
                    LeaveTypeId = l.LeaveTypeId,
                    LeaveTypeName = l.LeaveType.Name,
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    Reason = l.Reason,
                    Status = l.Status,
                    ApprovedById = l.ApprovedById,
                    ApprovedByName = l.ApprovedBy != null ? $"{l.ApprovedBy.FirstName} {l.ApprovedBy.LastName}" : null,
                    ApprovalDate = l.ApprovalDate,
                    Comments = l.Comments,
                    CreatedAt = l.CreatedAt
                })
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return Ok(leaves);
        }

        // GET: api/Leave/MyLeaves
        [HttpGet("MyLeaves")]
        public async Task<ActionResult<IEnumerable<LeaveDto>>> GetMyLeaves()
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

            var leaves = await _context.Leaves
                .Include(l => l.LeaveType)
                .Include(l => l.ApprovedBy)
                .Where(l => l.EmployeeId == employee.Id)
                .Select(l => new LeaveDto
                {
                    Id = l.Id,
                    EmployeeId = l.EmployeeId,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    LeaveTypeId = l.LeaveTypeId,
                    LeaveTypeName = l.LeaveType.Name,
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    Reason = l.Reason,
                    Status = l.Status,
                    ApprovedById = l.ApprovedById,
                    ApprovedByName = l.ApprovedBy != null ? $"{l.ApprovedBy.FirstName} {l.ApprovedBy.LastName}" : null,
                    ApprovalDate = l.ApprovalDate,
                    Comments = l.Comments,
                    CreatedAt = l.CreatedAt
                })
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return Ok(leaves);
        }

        // GET: api/Leave/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveDto>> GetLeave(Guid id)
        {
            var leave = await _context.Leaves
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .Include(l => l.ApprovedBy)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leave == null)
            {
                return NotFound();
            }

            // Check if the user is authorized to view this leave
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);

            // Only allow if the leave belongs to the employee or the user is an admin
            if (employee == null || (leave.EmployeeId != employee.Id && !User.IsInRole("Admin")))
            {
                return Forbid();
            }

            var leaveDto = new LeaveDto
            {
                Id = leave.Id,
                EmployeeId = leave.EmployeeId,
                EmployeeName = $"{leave.Employee.FirstName} {leave.Employee.LastName}",
                LeaveTypeId = leave.LeaveTypeId,
                LeaveTypeName = leave.LeaveType.Name,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                Reason = leave.Reason,
                Status = leave.Status,
                ApprovedById = leave.ApprovedById,
                ApprovedByName = leave.ApprovedBy != null ? $"{leave.ApprovedBy.FirstName} {leave.ApprovedBy.LastName}" : null,
                ApprovalDate = leave.ApprovalDate,
                Comments = leave.Comments,
                CreatedAt = leave.CreatedAt
            };

            return Ok(leaveDto);
        }

        // POST: api/Leave
        [HttpPost]
        public async Task<ActionResult<LeaveDto>> CreateLeave(CreateLeaveDto leaveDto)
        {
            // Get the current user
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

            // Check if leave type exists
            var leaveType = await _context.LeaveTypes.FindAsync(leaveDto.LeaveTypeId);
            if (leaveType == null)
            {
                return BadRequest("Leave type not found");
            }

            // Validate dates
            if (leaveDto.StartDate > leaveDto.EndDate)
            {
                return BadRequest("Start date cannot be after end date");
            }

            if (leaveDto.StartDate < DateTime.Now.Date)
            {
                return BadRequest("Cannot apply for leave in the past");
            }

            // Calculate number of days
            var days = (leaveDto.EndDate - leaveDto.StartDate).Days + 1;

            // Check leave balance
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == employee.Id && 
                                          lb.LeaveTypeId == leaveDto.LeaveTypeId && 
                                          lb.Year == DateTime.Now.Year);

            if (leaveBalance == null)
            {
                // Create a new leave balance if it doesn't exist
                leaveBalance = new LeaveBalance
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee.Id,
                    LeaveTypeId = leaveDto.LeaveTypeId,
                    AllottedDays = leaveType.DefaultDays,
                    UsedDays = 0,
                    Year = DateTime.Now.Year,
                    CreatedAt = DateTime.Now
                };

                _context.LeaveBalances.Add(leaveBalance);
                await _context.SaveChangesAsync();
            }

            if (leaveBalance.RemainingDays < days)
            {
                return BadRequest($"Insufficient leave balance. Available: {leaveBalance.RemainingDays}, Requested: {days}");
            }

            // Create the leave request
            var leave = new Leave
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                LeaveTypeId = leaveDto.LeaveTypeId,
                StartDate = leaveDto.StartDate,
                EndDate = leaveDto.EndDate,
                Reason = leaveDto.Reason,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Leaves.Add(leave);
            
            // Update leave balance
            leaveBalance.UsedDays += days;
            leaveBalance.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Create response DTO instead of returning the entity directly
            var responseDto = new LeaveDto
            {
                Id = leave.Id,
                EmployeeId = employee.Id,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                LeaveTypeId = leaveDto.LeaveTypeId,
                LeaveTypeName = leaveType.Name,
                StartDate = leaveDto.StartDate,
                EndDate = leaveDto.EndDate,
                Reason = leaveDto.Reason,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            return CreatedAtAction(nameof(GetLeave), new { id = leave.Id }, responseDto);
        }

        // PUT: api/Leave/Approve/5
        [HttpPut("Approve/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveLeave(Guid id, ApproveLeaveDto approveDto)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null)
            {
                return NotFound();
            }

            // Get the current user as the approver
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var approver = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (approver == null)
            {
                return BadRequest("Approver employee record not found");
            }

            // Update leave status
            leave.Status = approveDto.Approved ? "Approved" : "Rejected";
            leave.ApprovedById = approver.Id;
            leave.ApprovalDate = DateTime.Now;
            leave.Comments = approveDto.Comments;
            leave.UpdatedAt = DateTime.Now;

            // If rejected, update leave balance to return the days
            if (!approveDto.Approved)
            {
                var days = (leave.EndDate - leave.StartDate).Days + 1;
                var leaveBalance = await _context.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.EmployeeId == leave.EmployeeId && 
                                              lb.LeaveTypeId == leave.LeaveTypeId && 
                                              lb.Year == DateTime.Now.Year);

                if (leaveBalance != null)
                {
                    leaveBalance.UsedDays -= days;
                    leaveBalance.UpdatedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Leave/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLeave(Guid id)
        {
            var leave = await _context.Leaves.FindAsync(id);
            if (leave == null)
            {
                return NotFound();
            }

            // Get the current user
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);

            // Only allow if the leave belongs to the employee or the user is an admin
            if (employee == null || (leave.EmployeeId != employee.Id && !User.IsInRole("Admin")))
            {
                return Forbid();
            }

            // Only allow cancellation if the leave is still pending
            if (leave.Status != "Pending")
            {
                return BadRequest("Cannot cancel a leave that has already been processed");
            }

            // Update leave balance to return the days
            var days = (leave.EndDate - leave.StartDate).Days + 1;
            var leaveBalance = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb => lb.EmployeeId == leave.EmployeeId && 
                                          lb.LeaveTypeId == leave.LeaveTypeId && 
                                          lb.Year == DateTime.Now.Year);

            if (leaveBalance != null)
            {
                leaveBalance.UsedDays -= days;
                leaveBalance.UpdatedAt = DateTime.Now;
            }

            _context.Leaves.Remove(leave);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Leave/Types
        [HttpGet("Types")]
        public async Task<ActionResult<IEnumerable<LeaveTypeDto>>> GetLeaveTypes()
        {
            var leaveTypes = await _context.LeaveTypes
                .Select(lt => new LeaveTypeDto
                {
                    Id = lt.Id,
                    Name = lt.Name,
                    Description = lt.Description,
                    DefaultDays = lt.DefaultDays,
                    RequiresApproval = lt.RequiresApproval
                })
                .ToListAsync();

            return Ok(leaveTypes);
        }

        // GET: api/Leave/Balance
        [HttpGet("Balance")]
        public async Task<ActionResult<IEnumerable<LeaveBalanceDto>>> GetMyLeaveBalance()
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

            var leaveTypes = await _context.LeaveTypes.ToListAsync();
            var leaveBalances = await _context.LeaveBalances
                .Where(lb => lb.EmployeeId == employee.Id && lb.Year == DateTime.Now.Year)
                .ToListAsync();

            var result = new List<LeaveBalanceDto>();

            foreach (var leaveType in leaveTypes)
            {
                var balance = leaveBalances.FirstOrDefault(lb => lb.LeaveTypeId == leaveType.Id);

                if (balance == null)
                {
                    // If no balance record exists, create a default one
                    result.Add(new LeaveBalanceDto
                    {
                        LeaveTypeId = leaveType.Id,
                        LeaveTypeName = leaveType.Name,
                        AllottedDays = leaveType.DefaultDays,
                        UsedDays = 0,
                        RemainingDays = leaveType.DefaultDays,
                        Year = DateTime.Now.Year
                    });
                }
                else
                {
                    result.Add(new LeaveBalanceDto
                    {
                        Id = balance.Id,
                        LeaveTypeId = leaveType.Id,
                        LeaveTypeName = leaveType.Name,
                        AllottedDays = balance.AllottedDays,
                        UsedDays = balance.UsedDays,
                        RemainingDays = balance.RemainingDays,
                        Year = balance.Year
                    });
                }
            }

            return Ok(result);
        }
    }
}
