using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetHRSystem.Data;
using NetHRSystem.DTOs;
using NetHRSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetHRSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DepartmentController : ControllerBase
    {
        private readonly DataContext _context;

        public DepartmentController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Department
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
        {
            var departments = await _context.Departments
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    ManagerId = d.ManagerId,
                    EmployeeCount = d.Employees.Count
                })
                .ToListAsync();

            return Ok(departments);
        }

        // GET: api/Department/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DepartmentDetailDto>> GetDepartment(Guid id)
        {
            var department = await _context.Departments
                .Include(d => d.Employees)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return NotFound();
            }

            var manager = department.ManagerId.HasValue
                ? await _context.Employees.FindAsync(department.ManagerId.Value)
                : null;

            var departmentDto = new DepartmentDetailDto
            {
                Id = department.Id,
                Name = department.Name,
                Description = department.Description,
                ManagerId = department.ManagerId,
                ManagerName = manager != null ? $"{manager.FirstName} {manager.LastName}" : null,
                Employees = department.Employees.Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Email = e.Email,
                    Phone = e.Phone,
                    JobTitle = e.JobTitle,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = department.Name,
                    ManagerId = e.ManagerId,
                    UserId = e.UserId,
                    Username = e.User.Username
                }).ToList()
            };

            return Ok(departmentDto);
        }

        // POST: api/Department
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Department>> CreateDepartment(CreateDepartmentDto departmentDto)
        {
            // Check if manager exists if managerId is provided
            if (departmentDto.ManagerId.HasValue)
            {
                var manager = await _context.Employees.FindAsync(departmentDto.ManagerId.Value);
                if (manager == null)
                {
                    return BadRequest("Manager not found");
                }
            }

            var department = new Department
            {
                Id = Guid.NewGuid(),
                Name = departmentDto.Name,
                Description = departmentDto.Description,
                ManagerId = departmentDto.ManagerId
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, department);
        }

        // PUT: api/Department/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDepartment(Guid id, UpdateDepartmentDto departmentDto)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            // Check if manager exists if managerId is provided
            if (departmentDto.ManagerId.HasValue)
            {
                var manager = await _context.Employees.FindAsync(departmentDto.ManagerId.Value);
                if (manager == null)
                {
                    return BadRequest("Manager not found");
                }
            }

            department.Name = departmentDto.Name;
            department.Description = departmentDto.Description;
            department.ManagerId = departmentDto.ManagerId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartmentExists(id))
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

        // DELETE: api/Department/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDepartment(Guid id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }

            // Check if department has employees
            var hasEmployees = await _context.Employees.AnyAsync(e => e.DepartmentId == id);
            if (hasEmployees)
            {
                return BadRequest("Cannot delete department with employees. Please reassign employees first.");
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DepartmentExists(Guid id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}
