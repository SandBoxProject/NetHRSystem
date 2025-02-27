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
    public class SettingsController : ControllerBase
    {
        private readonly DataContext _context;

        public SettingsController(DataContext context)
        {
            _context = context;
        }

        // GET: api/Settings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SettingDto>>> GetSettings()
        {
            var settings = await _context.Settings
                .Select(s => new SettingDto
                {
                    Id = s.Id,
                    Key = s.Key,
                    Value = s.Value,
                    Description = s.Description,
                    Type = s.Type,
                    Category = s.Category,
                    IsReadOnly = s.IsReadOnly,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Key)
                .ToListAsync();

            return Ok(settings);
        }

        // GET: api/Settings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SettingDto>> GetSetting(Guid id)
        {
            var setting = await _context.Settings.FindAsync(id);

            if (setting == null)
            {
                return NotFound();
            }

            var settingDto = new SettingDto
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                Type = setting.Type,
                Category = setting.Category,
                IsReadOnly = setting.IsReadOnly,
                CreatedAt = setting.CreatedAt,
                UpdatedAt = setting.UpdatedAt
            };

            return Ok(settingDto);
        }

        // GET: api/Settings/ByKey/{key}
        [HttpGet("ByKey/{key}")]
        public async Task<ActionResult<SettingDto>> GetSettingByKey(string key)
        {
            var setting = await _context.Settings.FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                return NotFound();
            }

            var settingDto = new SettingDto
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                Type = setting.Type,
                Category = setting.Category,
                IsReadOnly = setting.IsReadOnly,
                CreatedAt = setting.CreatedAt,
                UpdatedAt = setting.UpdatedAt
            };

            return Ok(settingDto);
        }

        // GET: api/Settings/ByCategory/{category}
        [HttpGet("ByCategory/{category}")]
        public async Task<ActionResult<IEnumerable<SettingDto>>> GetSettingsByCategory(string category)
        {
            var settings = await _context.Settings
                .Where(s => s.Category == category)
                .Select(s => new SettingDto
                {
                    Id = s.Id,
                    Key = s.Key,
                    Value = s.Value,
                    Description = s.Description,
                    Type = s.Type,
                    Category = s.Category,
                    IsReadOnly = s.IsReadOnly,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                })
                .OrderBy(s => s.Key)
                .ToListAsync();

            return Ok(settings);
        }

        // PUT: api/Settings/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSetting(Guid id, UpdateSettingDto settingDto)
        {
            var setting = await _context.Settings.FindAsync(id);
            if (setting == null)
            {
                return NotFound();
            }

            if (setting.IsReadOnly)
            {
                return BadRequest("This setting is read-only and cannot be modified");
            }

            // Validate value based on type
            if (!IsValidSettingValue(settingDto.Value, setting.Type))
            {
                return BadRequest($"Invalid value format for setting type '{setting.Type}'");
            }

            setting.Value = settingDto.Value;
            setting.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SettingExists(id))
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

        // POST: api/Settings
        [HttpPost]
        public async Task<ActionResult<Setting>> CreateSetting(CreateSettingDto settingDto)
        {
            // Check if key already exists
            if (await _context.Settings.AnyAsync(s => s.Key == settingDto.Key))
            {
                return BadRequest("A setting with this key already exists");
            }

            // Validate value based on type
            if (!IsValidSettingValue(settingDto.Value, settingDto.Type))
            {
                return BadRequest($"Invalid value format for setting type '{settingDto.Type}'");
            }

            var setting = new Setting
            {
                Id = Guid.NewGuid(),
                Key = settingDto.Key,
                Value = settingDto.Value,
                Description = settingDto.Description,
                Type = settingDto.Type,
                Category = settingDto.Category,
                IsReadOnly = settingDto.IsReadOnly,
                CreatedAt = DateTime.Now
            };

            _context.Settings.Add(setting);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSetting), new { id = setting.Id }, setting);
        }

        // DELETE: api/Settings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSetting(Guid id)
        {
            var setting = await _context.Settings.FindAsync(id);
            if (setting == null)
            {
                return NotFound();
            }

            if (setting.IsReadOnly)
            {
                return BadRequest("This setting is read-only and cannot be deleted");
            }

            _context.Settings.Remove(setting);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Settings/Categories
        [HttpGet("Categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _context.Settings
                .Select(s => s.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        private bool SettingExists(Guid id)
        {
            return _context.Settings.Any(e => e.Id == id);
        }

        private bool IsValidSettingValue(string value, string type)
        {
            switch (type.ToLower())
            {
                case "string":
                    return true; // All strings are valid
                case "integer":
                    return int.TryParse(value, out _);
                case "decimal":
                    return decimal.TryParse(value, out _);
                case "boolean":
                    return bool.TryParse(value, out _);
                case "date":
                    return DateTime.TryParse(value, out _);
                case "json":
                    try
                    {
                        System.Text.Json.JsonDocument.Parse(value);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                default:
                    return true; // Unknown types are treated as strings
            }
        }
    }
}
