using Microsoft.EntityFrameworkCore;
using NetHRSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NetHRSystem.Data
{
    public static class DbInitializer
    {
        public static void Initialize(DataContext context)
        {
            context.Database.EnsureCreated();

            // Look for any users
            if (context.Users.Any())
            {
                return;   // DB has been seeded
            }

            SeedRoles(context);
            SeedPermissions(context);
            SeedRolePermissions(context);
            SeedUsers(context);
            SeedLeaveTypes(context);
            SeedClaimTypes(context);
            SeedDepartments(context);
            SeedSettings(context);
        }

        private static void SeedRoles(DataContext context)
        {
            var roles = new List<Role>
            {
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Admin",
                    Description = "Administrator with full access to all features",
                    CreatedAt = DateTime.Now
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "User",
                    Description = "Regular user with limited access",
                    CreatedAt = DateTime.Now
                },
                new Role
                {
                    Id = Guid.NewGuid(),
                    Name = "Developer",
                    Description = "Developer with access to technical features",
                    CreatedAt = DateTime.Now
                }
            };

            context.Roles.AddRange(roles);
            context.SaveChanges();
        }

        private static void SeedPermissions(DataContext context)
        {
            var modules = new[] { "Dashboard", "Employee", "Leave", "Claim", "Attendance", "Settings" };
            var actions = new[] { "View", "Create", "Edit", "Delete", "Approve" };

            var permissions = new List<Permission>();

            foreach (var module in modules)
            {
                foreach (var action in actions)
                {
                    permissions.Add(new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{action}{module}",
                        Description = $"Permission to {action.ToLower()} {module.ToLower()}",
                        Module = module,
                        Action = action,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            context.Permissions.AddRange(permissions);
            context.SaveChanges();
        }

        private static void SeedRolePermissions(DataContext context)
        {
            var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
            var userRole = context.Roles.FirstOrDefault(r => r.Name == "User");
            var developerRole = context.Roles.FirstOrDefault(r => r.Name == "Developer");
            var allPermissions = context.Permissions.ToList();

            var rolePermissions = new List<RolePermission>();

            // Admin has all permissions
            foreach (var permission in allPermissions)
            {
                rolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = adminRole.Id,
                    PermissionId = permission.Id,
                    CreatedAt = DateTime.Now
                });
            }

            // User has view permissions for all modules and create/edit for Leave and Claim
            var userPermissions = allPermissions.Where(p => 
                p.Action == "View" || 
                (p.Action == "Create" && (p.Module == "Leave" || p.Module == "Claim")) ||
                (p.Action == "Edit" && (p.Module == "Leave" || p.Module == "Claim"))
            ).ToList();

            foreach (var permission in userPermissions)
            {
                rolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = userRole.Id,
                    PermissionId = permission.Id,
                    CreatedAt = DateTime.Now
                });
            }

            // Developer has all permissions except Settings module
            var developerPermissions = allPermissions.Where(p => p.Module != "Settings").ToList();

            foreach (var permission in developerPermissions)
            {
                rolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = developerRole.Id,
                    PermissionId = permission.Id,
                    CreatedAt = DateTime.Now
                });
            }

            context.RolePermissions.AddRange(rolePermissions);
            context.SaveChanges();
        }

        private static void SeedUsers(DataContext context)
        {
            // Create password hash for admin user
            CreatePasswordHash("admin123", out byte[] passwordHash, out byte[] passwordSalt);

            var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
            var userRole = context.Roles.FirstOrDefault(r => r.Name == "User");
            var developerRole = context.Roles.FirstOrDefault(r => r.Name == "Developer");

            var users = new List<User>
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Role = "Admin"
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "user",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Role = "User"
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "developer",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Role = "Developer"
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();

            // Assign roles to users
            var userRoles = new List<UserRole>
            {
                new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = users[0].Id,
                    RoleId = adminRole.Id,
                    CreatedAt = DateTime.Now
                },
                new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = users[1].Id,
                    RoleId = userRole.Id,
                    CreatedAt = DateTime.Now
                },
                new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = users[2].Id,
                    RoleId = developerRole.Id,
                    CreatedAt = DateTime.Now
                }
            };

            context.UserRoles.AddRange(userRoles);
            context.SaveChanges();
        }

        private static void SeedLeaveTypes(DataContext context)
        {
            var leaveTypes = new List<LeaveType>
            {
                new LeaveType
                {
                    Id = Guid.NewGuid(),
                    Name = "Annual Leave",
                    Description = "Regular annual leave",
                    DefaultDays = 14,
                    RequiresApproval = true
                },
                new LeaveType
                {
                    Id = Guid.NewGuid(),
                    Name = "Sick Leave",
                    Description = "Leave for medical reasons",
                    DefaultDays = 10,
                    RequiresApproval = true
                },
                new LeaveType
                {
                    Id = Guid.NewGuid(),
                    Name = "Maternity Leave",
                    Description = "Leave for childbirth and childcare",
                    DefaultDays = 90,
                    RequiresApproval = true
                },
                new LeaveType
                {
                    Id = Guid.NewGuid(),
                    Name = "Paternity Leave",
                    Description = "Leave for fathers after childbirth",
                    DefaultDays = 7,
                    RequiresApproval = true
                },
                new LeaveType
                {
                    Id = Guid.NewGuid(),
                    Name = "Unpaid Leave",
                    Description = "Leave without pay",
                    DefaultDays = 0,
                    RequiresApproval = true
                }
            };

            context.LeaveTypes.AddRange(leaveTypes);
            context.SaveChanges();
        }

        private static void SeedClaimTypes(DataContext context)
        {
            var claimTypes = new List<ClaimType>
            {
                new ClaimType
                {
                    Id = Guid.NewGuid(),
                    Name = "Travel",
                    Description = "Travel related expenses",
                    MaximumAmount = 1000,
                    RequiresReceipt = true,
                    RequiresApproval = true
                },
                new ClaimType
                {
                    Id = Guid.NewGuid(),
                    Name = "Meals",
                    Description = "Meal expenses during business trips",
                    MaximumAmount = 500,
                    RequiresReceipt = true,
                    RequiresApproval = true
                },
                new ClaimType
                {
                    Id = Guid.NewGuid(),
                    Name = "Office Supplies",
                    Description = "Expenses for office supplies",
                    MaximumAmount = 200,
                    RequiresReceipt = true,
                    RequiresApproval = true
                },
                new ClaimType
                {
                    Id = Guid.NewGuid(),
                    Name = "Training",
                    Description = "Expenses for training and courses",
                    MaximumAmount = 2000,
                    RequiresReceipt = true,
                    RequiresApproval = true
                },
                new ClaimType
                {
                    Id = Guid.NewGuid(),
                    Name = "Other",
                    Description = "Other miscellaneous expenses",
                    MaximumAmount = 300,
                    RequiresReceipt = true,
                    RequiresApproval = true
                }
            };

            context.ClaimTypes.AddRange(claimTypes);
            context.SaveChanges();
        }

        private static void SeedDepartments(DataContext context)
        {
            var departments = new List<Department>
            {
                new Department
                {
                    Id = Guid.NewGuid(),
                    Name = "Human Resources",
                    Description = "HR department"
                },
                new Department
                {
                    Id = Guid.NewGuid(),
                    Name = "Information Technology",
                    Description = "IT department"
                },
                new Department
                {
                    Id = Guid.NewGuid(),
                    Name = "Finance",
                    Description = "Finance department"
                },
                new Department
                {
                    Id = Guid.NewGuid(),
                    Name = "Marketing",
                    Description = "Marketing department"
                },
                new Department
                {
                    Id = Guid.NewGuid(),
                    Name = "Operations",
                    Description = "Operations department"
                }
            };

            context.Departments.AddRange(departments);
            context.SaveChanges();
        }

        private static void SeedSettings(DataContext context)
        {
            var settings = new List<Setting>
            {
                new Setting
                {
                    Id = Guid.NewGuid(),
                    Key = "CompanyName",
                    Value = "HR Management System",
                    Description = "Name of the company",
                    Type = "string",
                    Category = "General",
                    IsReadOnly = false,
                    CreatedAt = DateTime.Now
                },
                new Setting
                {
                    Id = Guid.NewGuid(),
                    Key = "WorkingHoursPerDay",
                    Value = "8",
                    Description = "Standard working hours per day",
                    Type = "integer",
                    Category = "Attendance",
                    IsReadOnly = false,
                    CreatedAt = DateTime.Now
                },
                new Setting
                {
                    Id = Guid.NewGuid(),
                    Key = "WorkingDaysPerWeek",
                    Value = "5",
                    Description = "Standard working days per week",
                    Type = "integer",
                    Category = "Attendance",
                    IsReadOnly = false,
                    CreatedAt = DateTime.Now
                },
                new Setting
                {
                    Id = Guid.NewGuid(),
                    Key = "MaximumClaimAmountPerMonth",
                    Value = "5000",
                    Description = "Maximum claim amount per month per employee",
                    Type = "decimal",
                    Category = "Claims",
                    IsReadOnly = false,
                    CreatedAt = DateTime.Now
                },
                new Setting
                {
                    Id = Guid.NewGuid(),
                    Key = "DefaultLeaveApprover",
                    Value = "HR Manager",
                    Description = "Default approver for leave requests",
                    Type = "string",
                    Category = "Leave",
                    IsReadOnly = false,
                    CreatedAt = DateTime.Now
                },
                new Setting
                {
                    Id = Guid.NewGuid(),
                    Key = "DefaultClaimApprover",
                    Value = "Finance Manager",
                    Description = "Default approver for claim requests",
                    Type = "string",
                    Category = "Claims",
                    IsReadOnly = false,
                    CreatedAt = DateTime.Now
                },
                new Setting
                {
                    Id = Guid.NewGuid(),
                    Key = "SystemVersion",
                    Value = "1.0.0",
                    Description = "Current system version",
                    Type = "string",
                    Category = "System",
                    IsReadOnly = true,
                    CreatedAt = DateTime.Now
                }
            };

            context.Settings.AddRange(settings);
            context.SaveChanges();
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }
    }
}
