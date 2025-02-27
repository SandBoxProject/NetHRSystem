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
    public class EmployeeController : ControllerBase
    {
        private readonly DataContext _context;

        public EmployeeController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Employee
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.User)
                .Select(e => new EmployeeDto
                {
                    Id = e.Id,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Email = e.Email,
                    Phone = e.Phone,
                    JobTitle = e.JobTitle,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department != null ? e.Department.Name : null,
                    ManagerId = e.ManagerId,
                    UserId = e.UserId,
                    Username = e.User.Username
                })
                .ToListAsync();

            return Ok(employees);
        }

        // GET: api/Employee/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDetailDto>> GetEmployee(Guid id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            var employeeDto = new EmployeeDetailDto
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                Phone = employee.Phone,
                DateOfBirth = employee.DateOfBirth,
                HireDate = employee.HireDate,
                Address = employee.Address,
                City = employee.City,
                State = employee.State,
                ZipCode = employee.ZipCode,
                Country = employee.Country,
                JobTitle = employee.JobTitle,
                Salary = employee.Salary,
                DepartmentId = employee.DepartmentId,
                DepartmentName = employee.Department?.Name,
                ManagerId = employee.ManagerId,
                ManagerName = employee.Manager != null ? $"{employee.Manager.FirstName} {employee.Manager.LastName}" : null,
                UserId = employee.UserId,
                Username = employee.User.Username
            };

            return Ok(employeeDto);
        }

        // POST: api/Employee
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Employee>> CreateEmployee(CreateEmployeeDto employeeDto)
        {
            // Check if user exists
            var user = await _context.Users.FindAsync(employeeDto.UserId);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            // Check if department exists if departmentId is provided
            if (employeeDto.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(employeeDto.DepartmentId.Value);
                if (department == null)
                {
                    return BadRequest("Department not found");
                }
            }

            // Check if manager exists if managerId is provided
            if (employeeDto.ManagerId.HasValue)
            {
                var manager = await _context.Employees.FindAsync(employeeDto.ManagerId.Value);
                if (manager == null)
                {
                    return BadRequest("Manager not found");
                }
            }

            // Check if user already has an employee record
            var existingEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == employeeDto.UserId);
            if (existingEmployee != null)
            {
                return BadRequest("User already has an employee record");
            }

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                FirstName = employeeDto.FirstName,
                LastName = employeeDto.LastName,
                Email = employeeDto.Email,
                Phone = employeeDto.Phone,
                DateOfBirth = employeeDto.DateOfBirth,
                HireDate = employeeDto.HireDate,
                Address = employeeDto.Address,
                City = employeeDto.City,
                State = employeeDto.State,
                ZipCode = employeeDto.ZipCode,
                Country = employeeDto.Country,
                JobTitle = employeeDto.JobTitle,
                Salary = employeeDto.Salary,
                DepartmentId = employeeDto.DepartmentId,
                ManagerId = employeeDto.ManagerId,
                UserId = employeeDto.UserId
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }

        // PUT: api/Employee/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmployee(Guid id, UpdateEmployeeDto employeeDto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            // Check if department exists if departmentId is provided
            if (employeeDto.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(employeeDto.DepartmentId.Value);
                if (department == null)
                {
                    return BadRequest("Department not found");
                }
            }

            // Check if manager exists if managerId is provided
            if (employeeDto.ManagerId.HasValue)
            {
                var manager = await _context.Employees.FindAsync(employeeDto.ManagerId.Value);
                if (manager == null)
                {
                    return BadRequest("Manager not found");
                }
            }

            // Update employee properties
            employee.FirstName = employeeDto.FirstName;
            employee.LastName = employeeDto.LastName;
            employee.Email = employeeDto.Email;
            employee.Phone = employeeDto.Phone;
            employee.DateOfBirth = employeeDto.DateOfBirth;
            employee.HireDate = employeeDto.HireDate;
            employee.Address = employeeDto.Address;
            employee.City = employeeDto.City;
            employee.State = employeeDto.State;
            employee.ZipCode = employeeDto.ZipCode;
            employee.Country = employeeDto.Country;
            employee.JobTitle = employeeDto.JobTitle;
            employee.Salary = employeeDto.Salary;
            employee.DepartmentId = employeeDto.DepartmentId;
            employee.ManagerId = employeeDto.ManagerId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(id))
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

        // POST: api/Employee/CreateMyProfile
        [HttpPost("CreateMyProfile")]
        [Authorize(Roles = "User")]
        public async Task<ActionResult<Employee>> CreateMyProfile(CreateMyProfileDto profileDto)
        {
            // Get the current user
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            // Check if user already has an employee record
            var existingEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (existingEmployee != null)
            {
                return BadRequest("You already have an employee profile");
            }

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                FirstName = profileDto.FirstName,
                LastName = profileDto.LastName,
                Email = profileDto.Email,
                Phone = profileDto.Phone,
                DateOfBirth = profileDto.DateOfBirth,
                HireDate = DateTime.Now, // Default to current date
                Address = profileDto.Address,
                City = profileDto.City,
                State = profileDto.State,
                ZipCode = profileDto.ZipCode,
                Country = profileDto.Country,
                JobTitle = "Employee", // Default job title
                Salary = 0, // Will be set by admin later
                UserId = user.Id
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }

        // DELETE: api/Employee/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(Guid id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EmployeeExists(Guid id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }
    }
}
