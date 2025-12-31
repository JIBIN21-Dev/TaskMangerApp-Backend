using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManger.Data;
using TaskManger.Models;

namespace TaskManger.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly TaskDb _context;

        public AdminController(TaskDb context)
        {
            _context = context;
        }

        // GET: api/admin/statistics
        [HttpGet("statistics")]
        public async Task<IActionResult> GetAdminStatistics()
        {
            // Only allow specific admin username
            var currentUser = User.FindFirst(ClaimTypes.Name)?.Value;

            if (currentUser != "jibin2121") // Change to YOUR username!
            {
                return Forbid(); // Not admin!
            }

            // Get statistics
            var totalUsers = await _context.Users.CountAsync();
            var totalTasks = await _context.TaskNames.CountAsync();
            var completedTasks = await _context.TaskNames.CountAsync(t => t.IsCompleted);

            var userRegistrations = await _context.Users
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Date)
                .Take(7)
                .ToListAsync();

            var activeUsers = await _context.Users
                .Where(u => _context.TaskNames.Any(t => t.UserId == u.Id))
                .CountAsync();

            return Ok(new
            {
                success = true,
                statistics = new
                {
                    totalUsers = totalUsers,
                    totalTasks = totalTasks,
                    completedTasks = completedTasks,
                    activeUsers = activeUsers,
                    recentRegistrations = userRegistrations
                }
            });
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var currentUser = User.FindFirst(ClaimTypes.Name)?.Value;

            if (currentUser != "jibin2121") // Change to YOUR username!
            {
                return Forbid();
            }

            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Name,
                    u.CreatedAt,
                    TaskCount = _context.TaskNames.Count(t => t.UserId == u.Id)
                })
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(new { success = true, users = users });
        }
    }
}