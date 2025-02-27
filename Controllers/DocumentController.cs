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
    public class DocumentController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IUserService _userService;

        public DocumentController(DataContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        // GET: api/Document
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetAllDocuments()
        {
            var documents = await _context.Documents
                .Include(d => d.Employee)
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    EmployeeId = d.EmployeeId,
                    EmployeeName = $"{d.Employee.FirstName} {d.Employee.LastName}",
                    Title = d.Title,
                    Description = d.Description,
                    FileUrl = d.FileUrl,
                    FileType = d.FileType,
                    FileSize = d.FileSize,
                    IsPublic = d.IsPublic,
                    UploadDate = d.UploadDate,
                    ExpiryDate = d.ExpiryDate,
                    Tags = d.Tags,
                    Status = d.Status
                })
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();

            return Ok(documents);
        }

        // GET: api/Document/MyDocuments
        [HttpGet("MyDocuments")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetMyDocuments()
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

            var documents = await _context.Documents
                .Where(d => d.EmployeeId == employee.Id)
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    EmployeeId = d.EmployeeId,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    Title = d.Title,
                    Description = d.Description,
                    FileUrl = d.FileUrl,
                    FileType = d.FileType,
                    FileSize = d.FileSize,
                    IsPublic = d.IsPublic,
                    UploadDate = d.UploadDate,
                    ExpiryDate = d.ExpiryDate,
                    Tags = d.Tags,
                    Status = d.Status
                })
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();

            return Ok(documents);
        }

        // GET: api/Document/Public
        [HttpGet("Public")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> GetPublicDocuments()
        {
            var documents = await _context.Documents
                .Include(d => d.Employee)
                .Where(d => d.IsPublic && d.Status == "Active")
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    EmployeeId = d.EmployeeId,
                    EmployeeName = $"{d.Employee.FirstName} {d.Employee.LastName}",
                    Title = d.Title,
                    Description = d.Description,
                    FileUrl = d.FileUrl,
                    FileType = d.FileType,
                    FileSize = d.FileSize,
                    IsPublic = d.IsPublic,
                    UploadDate = d.UploadDate,
                    ExpiryDate = d.ExpiryDate,
                    Tags = d.Tags,
                    Status = d.Status
                })
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();

            return Ok(documents);
        }

        // GET: api/Document/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentDto>> GetDocument(Guid id)
        {
            var document = await _context.Documents
                .Include(d => d.Employee)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return NotFound();
            }

            // Check if the user is authorized to view this document
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);

            // Allow if the document is public, belongs to the employee, or the user is an admin
            if (!document.IsPublic && (employee == null || (document.EmployeeId != employee.Id && !User.IsInRole("Admin"))))
            {
                return Forbid();
            }

            var documentDto = new DocumentDto
            {
                Id = document.Id,
                EmployeeId = document.EmployeeId,
                EmployeeName = $"{document.Employee.FirstName} {document.Employee.LastName}",
                Title = document.Title,
                Description = document.Description,
                FileUrl = document.FileUrl,
                FileType = document.FileType,
                FileSize = document.FileSize,
                IsPublic = document.IsPublic,
                UploadDate = document.UploadDate,
                ExpiryDate = document.ExpiryDate,
                Tags = document.Tags,
                Status = document.Status
            };

            return Ok(documentDto);
        }

        // POST: api/Document
        [HttpPost]
        public async Task<ActionResult<Document>> CreateDocument(CreateDocumentDto documentDto)
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

            // Validate file URL
            if (string.IsNullOrEmpty(documentDto.FileUrl))
            {
                return BadRequest("File URL is required");
            }

            // Create the document
            var document = new Document
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                Title = documentDto.Title,
                Description = documentDto.Description,
                FileUrl = documentDto.FileUrl,
                FileType = documentDto.FileType,
                FileSize = documentDto.FileSize,
                IsPublic = documentDto.IsPublic,
                UploadDate = DateTime.Now,
                ExpiryDate = documentDto.ExpiryDate,
                Tags = documentDto.Tags,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
        }

        // PUT: api/Document/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(Guid id, UpdateDocumentDto documentDto)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            // Check if the user is authorized to update this document
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);

            // Only allow if the document belongs to the employee or the user is an admin
            if (employee == null || (document.EmployeeId != employee.Id && !User.IsInRole("Admin")))
            {
                return Forbid();
            }

            document.Title = documentDto.Title;
            document.Description = documentDto.Description;
            document.IsPublic = documentDto.IsPublic;
            document.ExpiryDate = documentDto.ExpiryDate;
            document.Tags = documentDto.Tags;
            document.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DocumentExists(id))
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

        // DELETE: api/Document/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(Guid id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            // Check if the user is authorized to delete this document
            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);

            // Only allow if the document belongs to the employee or the user is an admin
            if (employee == null || (document.EmployeeId != employee.Id && !User.IsInRole("Admin")))
            {
                return Forbid();
            }

            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Document/Search
        [HttpGet("Search")]
        public async Task<ActionResult<IEnumerable<DocumentDto>>> SearchDocuments([FromQuery] string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return BadRequest("Search term is required");
            }

            var username = _userService.GetUserName();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);

            var query = _context.Documents
                .Include(d => d.Employee)
                .AsQueryable();

            // If not admin, only show public documents or own documents
            if (!User.IsInRole("Admin"))
            {
                if (employee != null)
                {
                    query = query.Where(d => d.IsPublic || d.EmployeeId == employee.Id);
                }
                else
                {
                    query = query.Where(d => d.IsPublic);
                }
            }

            // Search in title, description, and tags
            var searchTermLower = searchTerm.ToLower();
            query = query.Where(d => 
                d.Title.ToLower().Contains(searchTermLower) || 
                d.Description.ToLower().Contains(searchTermLower) || 
                d.Tags.ToLower().Contains(searchTermLower));

            var documents = await query
                .Select(d => new DocumentDto
                {
                    Id = d.Id,
                    EmployeeId = d.EmployeeId,
                    EmployeeName = $"{d.Employee.FirstName} {d.Employee.LastName}",
                    Title = d.Title,
                    Description = d.Description,
                    FileUrl = d.FileUrl,
                    FileType = d.FileType,
                    FileSize = d.FileSize,
                    IsPublic = d.IsPublic,
                    UploadDate = d.UploadDate,
                    ExpiryDate = d.ExpiryDate,
                    Tags = d.Tags,
                    Status = d.Status
                })
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();

            return Ok(documents);
        }

        private bool DocumentExists(Guid id)
        {
            return _context.Documents.Any(e => e.Id == id);
        }
    }
}
