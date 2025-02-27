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
    public class ClaimController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IUserService _userService;

        public ClaimController(DataContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/Claim
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ClaimDto>>> GetAllClaims()
        {
            var claims = await _context.Claims
                .Include(c => c.Employee)
                .Include(c => c.ClaimType)
                .Include(c => c.ApprovedBy)
                .Select(c => new ClaimDto
                {
                    Id = c.Id,
                    EmployeeId = c.EmployeeId,
                    EmployeeName = $"{c.Employee.FirstName} {c.Employee.LastName}",
                    ClaimTypeId = c.ClaimTypeId,
                    ClaimTypeName = c.ClaimType.Name,
                    Title = c.Title,
                    Description = c.Description,
                    Amount = c.Amount,
                    ClaimDate = c.ClaimDate,
                    Status = c.Status,
                    ApprovedById = c.ApprovedById,
                    ApprovedByName = c.ApprovedBy != null ? $"{c.ApprovedBy.FirstName} {c.ApprovedBy.LastName}" : null,
                    ApprovalDate = c.ApprovalDate,
                    Comments = c.Comments,
                    ReceiptUrl = c.ReceiptUrl,
                    CreatedAt = c.CreatedAt
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(claims);
        }

        // GET: api/Claim/MyClaims
        [HttpGet("MyClaims")]
        public async Task<ActionResult<IEnumerable<ClaimDto>>> GetMyClaims()
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

            var claims = await _context.Claims
                .Include(c => c.ClaimType)
                .Include(c => c.ApprovedBy)
                .Where(c => c.EmployeeId == employee.Id)
                .Select(c => new ClaimDto
                {
                    Id = c.Id,
                    EmployeeId = c.EmployeeId,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    ClaimTypeId = c.ClaimTypeId,
                    ClaimTypeName = c.ClaimType.Name,
                    Title = c.Title,
                    Description = c.Description,
                    Amount = c.Amount,
                    ClaimDate = c.ClaimDate,
                    Status = c.Status,
                    ApprovedById = c.ApprovedById,
                    ApprovedByName = c.ApprovedBy != null ? $"{c.ApprovedBy.FirstName} {c.ApprovedBy.LastName}" : null,
                    ApprovalDate = c.ApprovalDate,
                    Comments = c.Comments,
                    ReceiptUrl = c.ReceiptUrl,
                    CreatedAt = c.CreatedAt
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(claims);
        }

        // GET: api/Claim/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ClaimDto>> GetClaim(Guid id)
        {
            var claim = await _context.Claims
                .Include(c => c.Employee)
                .Include(c => c.ClaimType)
                .Include(c => c.ApprovedBy)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null)
            {
                return NotFound();
            }

            // Check if the user is authorized to view this claim
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);

            // Only allow if the claim belongs to the employee or the user is an admin
            if (employee == null || (claim.EmployeeId != employee.Id && !User.IsInRole("Admin")))
            {
                return Forbid();
            }

            var claimDto = new ClaimDto
            {
                Id = claim.Id,
                EmployeeId = claim.EmployeeId,
                EmployeeName = $"{claim.Employee.FirstName} {claim.Employee.LastName}",
                ClaimTypeId = claim.ClaimTypeId,
                ClaimTypeName = claim.ClaimType.Name,
                Title = claim.Title,
                Description = claim.Description,
                Amount = claim.Amount,
                ClaimDate = claim.ClaimDate,
                Status = claim.Status,
                ApprovedById = claim.ApprovedById,
                ApprovedByName = claim.ApprovedBy != null ? $"{claim.ApprovedBy.FirstName} {claim.ApprovedBy.LastName}" : null,
                ApprovalDate = claim.ApprovalDate,
                Comments = claim.Comments,
                ReceiptUrl = claim.ReceiptUrl,
                CreatedAt = claim.CreatedAt
            };

            return Ok(claimDto);
        }

        // POST: api/Claim
        [HttpPost]
        public async Task<ActionResult<Claim>> CreateClaim(CreateClaimDto claimDto)
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

            // Check if claim type exists
            var claimType = await _context.ClaimTypes.FindAsync(claimDto.ClaimTypeId);
            if (claimType == null)
            {
                return BadRequest("Claim type not found");
            }

            // Validate amount
            if (claimDto.Amount <= 0)
            {
                return BadRequest("Amount must be greater than zero");
            }

            if (claimDto.Amount > claimType.MaximumAmount)
            {
                return BadRequest($"Amount exceeds maximum allowed for this claim type ({claimType.MaximumAmount})");
            }

            // Validate date
            if (claimDto.ClaimDate > DateTime.Now)
            {
                return BadRequest("Claim date cannot be in the future");
            }

            // Check if receipt is required but not provided
            if (claimType.RequiresReceipt && string.IsNullOrEmpty(claimDto.ReceiptUrl))
            {
                return BadRequest("Receipt is required for this claim type");
            }

            // Create the claim
            var claim = new Claim
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                ClaimTypeId = claimDto.ClaimTypeId,
                Title = claimDto.Title,
                Description = claimDto.Description,
                Amount = claimDto.Amount,
                ClaimDate = claimDto.ClaimDate,
                Status = "Pending",
                ReceiptUrl = claimDto.ReceiptUrl,
                CreatedAt = DateTime.Now
            };

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetClaim), new { id = claim.Id }, claim);
        }

        // PUT: api/Claim/Approve/5
        [HttpPut("Approve/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveClaim(Guid id, ApproveClaimDto approveDto)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
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

            // Update claim status
            claim.Status = approveDto.Approved ? "Approved" : "Rejected";
            claim.ApprovedById = approver.Id;
            claim.ApprovalDate = DateTime.Now;
            claim.Comments = approveDto.Comments;
            claim.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Claim/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClaim(Guid id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim == null)
            {
                return NotFound();
            }

            // Get the current user
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);

            // Only allow if the claim belongs to the employee or the user is an admin
            if (employee == null || (claim.EmployeeId != employee.Id && !User.IsInRole("Admin")))
            {
                return Forbid();
            }

            // Only allow cancellation if the claim is still pending
            if (claim.Status != "Pending")
            {
                return BadRequest("Cannot cancel a claim that has already been processed");
            }

            _context.Claims.Remove(claim);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Claim/Types
        [HttpGet("Types")]
        public async Task<ActionResult<IEnumerable<ClaimTypeDto>>> GetClaimTypes()
        {
            var claimTypes = await _context.ClaimTypes
                .Select(ct => new ClaimTypeDto
                {
                    Id = ct.Id,
                    Name = ct.Name,
                    Description = ct.Description,
                    MaximumAmount = ct.MaximumAmount,
                    RequiresReceipt = ct.RequiresReceipt,
                    RequiresApproval = ct.RequiresApproval
                })
                .ToListAsync();

            return Ok(claimTypes);
        }

        // GET: api/Claim/Summary
        [HttpGet("Summary")]
        public async Task<ActionResult<ClaimSummaryDto>> GetClaimSummary()
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

            var currentYear = DateTime.Now.Year;
            var claims = await _context.Claims
                .Where(c => c.EmployeeId == employee.Id && c.ClaimDate.Year == currentYear)
                .ToListAsync();

            var summary = new ClaimSummaryDto
            {
                TotalClaims = claims.Count,
                TotalAmount = claims.Sum(c => c.Amount),
                ApprovedClaims = claims.Count(c => c.Status == "Approved"),
                ApprovedAmount = claims.Where(c => c.Status == "Approved").Sum(c => c.Amount),
                PendingClaims = claims.Count(c => c.Status == "Pending"),
                PendingAmount = claims.Where(c => c.Status == "Pending").Sum(c => c.Amount),
                RejectedClaims = claims.Count(c => c.Status == "Rejected"),
                RejectedAmount = claims.Where(c => c.Status == "Rejected").Sum(c => c.Amount)
            };

            return Ok(summary);
        }
    }
}
