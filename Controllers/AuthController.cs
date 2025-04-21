using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NetHRSystem.Data;
using NetHRSystem.DTOs;
using NetHRSystem.Models;
using NetHRSystem.Services.UserService;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace SampleConnectiom.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public static User user = new User();
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;
        private readonly DataContext _dataContext;

        public AuthController(IConfiguration configuration, IUserService _userService, DataContext dataContext)
        {
            this._configuration = configuration;
            this._userService = _userService;
            this._dataContext = dataContext;
        }

        [HttpGet("GetUserDetail"), Authorize]
        public ActionResult<object> GetMe()
        {
            var userName = _userService.GetUserName();
            return Ok(userName);

            //var userName = User?.Identity?.Name;
            //var userName2 = User.FindFirstValue(ClaimTypes.Name);
            //var role = User.FindFirst(ClaimTypes.Role);

            //return Ok(new {userName, userName2, role});
        }

        [HttpPost("Register")]
        public async Task<ActionResult<User>> Register(UserDto request)
        {
            // Check if username exists
            if (await _dataContext.Users.AnyAsync(u => u.Username == request.UserName))
            {
                return BadRequest("User already exists.");
            }

            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

            // Get the "User" role
            var userRole = await _dataContext.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (userRole == null)
            {
                return BadRequest("Default role not found. Please contact administrator.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.UserName,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Role = "User" // Assign default role
            };

            _dataContext.Users.Add(user);
            await _dataContext.SaveChangesAsync();

            // Assign the user role
            var userRoleAssignment = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = userRole.Id,
                CreatedAt = DateTime.Now
            };

            _dataContext.UserRoles.Add(userRoleAssignment);
            await _dataContext.SaveChangesAsync();

            // Create a basic employee record for the user
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                FirstName = "New",
                LastName = "Employee",
                Email = $"{request.UserName}@example.com",
                UserId = user.Id,
                HireDate = DateTime.Now,
                JobTitle = "Employee",
                DateOfBirth = new DateTime(2000, 1, 1) // Default date of birth
            };

            _dataContext.Employees.Add(employee);
            await _dataContext.SaveChangesAsync();

            return Ok(new { message = "User registered successfully" });
        }

        [HttpPost("Login")]
        public async Task<ActionResult<string>> Login(UserDto request)
        {
            var user = await _dataContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.Username == request.UserName);

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            if(!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest("Wrong Password");
            }

            string token = CreateToken(user);
            
            // Return user info along with token
            return Ok(new { 
                token = token,
                username = user.Username,
                role = user.Role,
                roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            });
        }

        private string CreateToken(User user)
        {
            List<System.Security.Claims.Claim> claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(ClaimTypes.Name, user.Username),
                new System.Security.Claims.Claim(ClaimTypes.Role, user.Role)
            };

            // Add role claims
            foreach (var userRole in user.UserRoles)
            {
                claims.Add(new System.Security.Claims.Claim(ClaimTypes.Role, userRole.Role.Name));
                
                // Add permission claims
                foreach (var rolePermission in userRole.Role.RolePermissions)
                {
                    claims.Add(new System.Security.Claims.Claim("Permission", rolePermission.Permission.Name));
                }
            }

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value));

            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: cred);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512()) { 
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }
    }
}
