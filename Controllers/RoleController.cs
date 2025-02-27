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
    [Authorize(Roles = "Admin")]
    public class RoleController : ControllerBase
    {
        private readonly DataContext _context;

        public RoleController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Role
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            var roles = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsDefault = r.IsDefault,
                    IsSystem = r.IsSystem,
                    Permissions = r.RolePermissions.Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name,
                        Description = rp.Permission.Description,
                        Module = rp.Permission.Module
                    }).ToList(),
                    UserCount = _context.UserRoles.Count(ur => ur.RoleId == r.Id)
                })
                .ToListAsync();

            return Ok(roles);
        }

        // GET: api/Role/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoleDto>> GetRole(Guid id)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsDefault = role.IsDefault,
                IsSystem = role.IsSystem,
                Permissions = role.RolePermissions.Select(rp => new PermissionDto
                {
                    Id = rp.Permission.Id,
                    Name = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    Module = rp.Permission.Module
                }).ToList(),
                UserCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == role.Id)
            };

            return Ok(roleDto);
        }

        // POST: api/Role
        [HttpPost]
        public async Task<ActionResult<Role>> CreateRole(CreateRoleDto roleDto)
        {
            // Check if role with the same name already exists
            if (await _context.Roles.AnyAsync(r => r.Name == roleDto.Name))
            {
                return BadRequest("A role with this name already exists");
            }

            // Validate permissions
            if (roleDto.PermissionIds != null && roleDto.PermissionIds.Any())
            {
                foreach (var permissionId in roleDto.PermissionIds)
                {
                    if (!await _context.Permissions.AnyAsync(p => p.Id == permissionId))
                    {
                        return BadRequest($"Permission with ID {permissionId} does not exist");
                    }
                }
            }

            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = roleDto.Name,
                Description = roleDto.Description,
                IsDefault = roleDto.IsDefault,
                IsSystem = false, // User-created roles are never system roles
                CreatedAt = DateTime.Now
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Add permissions to the role
            if (roleDto.PermissionIds != null && roleDto.PermissionIds.Any())
            {
                foreach (var permissionId in roleDto.PermissionIds)
                {
                    var rolePermission = new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId,
                        CreatedAt = DateTime.Now
                    };
                    _context.RolePermissions.Add(rolePermission);
                }
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
        }

        // PUT: api/Role/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(Guid id, UpdateRoleDto roleDto)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Cannot modify system roles
            if (role.IsSystem)
            {
                return BadRequest("System roles cannot be modified");
            }

            // Check if the updated name conflicts with an existing role
            if (await _context.Roles.AnyAsync(r => r.Name == roleDto.Name && r.Id != id))
            {
                return BadRequest("A role with this name already exists");
            }

            role.Name = roleDto.Name;
            role.Description = roleDto.Description;
            role.IsDefault = roleDto.IsDefault;
            role.UpdatedAt = DateTime.Now;

            // Update permissions if provided
            if (roleDto.PermissionIds != null)
            {
                // Validate permissions
                foreach (var permissionId in roleDto.PermissionIds)
                {
                    if (!await _context.Permissions.AnyAsync(p => p.Id == permissionId))
                    {
                        return BadRequest($"Permission with ID {permissionId} does not exist");
                    }
                }

                // Remove existing permissions
                var existingPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == id)
                    .ToListAsync();

                _context.RolePermissions.RemoveRange(existingPermissions);

                // Add new permissions
                foreach (var permissionId in roleDto.PermissionIds)
                {
                    var rolePermission = new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId,
                        CreatedAt = DateTime.Now
                    };
                    _context.RolePermissions.Add(rolePermission);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoleExists(id))
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

        // DELETE: api/Role/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Cannot delete system roles
            if (role.IsSystem)
            {
                return BadRequest("System roles cannot be deleted");
            }

            // Check if the role is assigned to any users
            if (await _context.UserRoles.AnyAsync(ur => ur.RoleId == id))
            {
                return BadRequest("Cannot delete a role that is assigned to users");
            }

            // Remove role permissions
            var rolePermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .ToListAsync();

            _context.RolePermissions.RemoveRange(rolePermissions);
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Role/Permissions
        [HttpGet("Permissions")]
        public async Task<ActionResult<IEnumerable<PermissionDto>>> GetPermissions()
        {
            var permissions = await _context.Permissions
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Module = p.Module
                })
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Name)
                .ToListAsync();

            return Ok(permissions);
        }

        // GET: api/Role/Users/{roleId}
        [HttpGet("Users/{roleId}")]
        public async Task<ActionResult<IEnumerable<RoleUserDto>>> GetUsersInRole(Guid roleId)
        {
            if (!await _context.Roles.AnyAsync(r => r.Id == roleId))
            {
                return NotFound("Role not found");
            }

            var users = await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Include(ur => ur.User)
                .Select(ur => new RoleUserDto
                {
                    Id = ur.User.Id,
                    Username = ur.User.Username,
                    Email = ur.User.Email,
                    FirstName = ur.User.FirstName,
                    LastName = ur.User.LastName,
                    IsActive = ur.User.IsActive,
                    CreatedAt = ur.User.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // POST: api/Role/AssignToUser
        [HttpPost("AssignToUser")]
        public async Task<IActionResult> AssignRoleToUser(AssignRoleDto assignRoleDto)
        {
            var user = await _context.Users.FindAsync(assignRoleDto.UserId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var role = await _context.Roles.FindAsync(assignRoleDto.RoleId);
            if (role == null)
            {
                return NotFound("Role not found");
            }

            // Check if the user already has this role
            if (await _context.UserRoles.AnyAsync(ur => ur.UserId == assignRoleDto.UserId && ur.RoleId == assignRoleDto.RoleId))
            {
                return BadRequest("User already has this role");
            }

            var userRole = new UserRole
            {
                UserId = assignRoleDto.UserId,
                RoleId = assignRoleDto.RoleId,
                CreatedAt = DateTime.Now
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Role/RemoveFromUser
        [HttpDelete("RemoveFromUser")]
        public async Task<IActionResult> RemoveRoleFromUser(AssignRoleDto removeRoleDto)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == removeRoleDto.UserId && ur.RoleId == removeRoleDto.RoleId);

            if (userRole == null)
            {
                return NotFound("User does not have this role");
            }

            // Check if this is the user's only role
            var userRoleCount = await _context.UserRoles.CountAsync(ur => ur.UserId == removeRoleDto.UserId);
            if (userRoleCount <= 1)
            {
                return BadRequest("Cannot remove the user's only role");
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RoleExists(Guid id)
        {
            return _context.Roles.Any(e => e.Id == id);
        }
    }
}
