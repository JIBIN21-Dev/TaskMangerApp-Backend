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
    public class TasksController : ControllerBase
    {
        private readonly TaskDb _context;

        public TasksController(TaskDb context)
        {
            _context = context;
        }

        public class CreateTaskRequest
        {
            public string Title { get; set; } = string.Empty;
            public string? Description { get; set; }
            public DateTime? DueDate { get; set; }
        }

        public class UpdateTaskRequest
        {
            public string? Title { get; set; }
            public string? Description { get; set; }
            public DateTime? DueDate { get; set; }
            public bool? IsCompleted { get; set; }
        }

        // ✅ FIXED: Returns ALL tasks for everyone (no user filtering)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTasks([FromQuery] string filter = "all")
        {
            var query = _context.TaskNames.AsQueryable();

            if (filter == "pending")
            {
                query = query.Where(t => !t.IsCompleted);
            }
            else if (filter == "completed")
            {
                query = query.Where(t => t.IsCompleted);
            }

            var tasks = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.DueDate,
                    t.IsCompleted,
                    t.CreatedAt,
                    t.UserId
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = tasks.Count,
                tasks = tasks
            });
        }

        // ✅ FIXED: Returns any task by ID (no user check)
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTask(int id)
        {
            var task = await _context.TaskNames
                .Where(t => t.Id == id)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.DueDate,
                    t.IsCompleted,
                    t.CreatedAt,
                    t.UserId
                })
                .FirstOrDefaultAsync();

            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            return Ok(new { success = true, task = task });
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var task = new TaskName
            {
                Title = request.Title,
                Description = request.Description,
                DueDate = request.DueDate,
                IsCompleted = false,
                CreatedAt = DateTime.Now,
                UserId = userId
            };

            _context.TaskNames.Add(task);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, new
            {
                success = true,
                message = "Task created successfully",
                task = new
                {
                    task.Id,
                    task.Title,
                    task.Description,
                    task.DueDate,
                    task.IsCompleted,
                    task.CreatedAt
                }
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var task = await _context.TaskNames
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            if (!string.IsNullOrEmpty(request.Title))
                task.Title = request.Title;

            if (request.Description != null)
                task.Description = request.Description;

            if (request.DueDate.HasValue)
                task.DueDate = request.DueDate;

            if (request.IsCompleted.HasValue)
                task.IsCompleted = request.IsCompleted.Value;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Task updated successfully",
                task = new
                {
                    task.Id,
                    task.Title,
                    task.Description,
                    task.DueDate,
                    task.IsCompleted,
                    task.CreatedAt
                }
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var task = await _context.TaskNames
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            _context.TaskNames.Remove(task);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Task deleted successfully"
            });
        }

        [HttpPatch("{id}/complete")]
        public async Task<IActionResult> MarkComplete(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var task = await _context.TaskNames
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            task.IsCompleted = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Task marked as complete",
                task = new
                {
                    task.Id,
                    task.Title,
                    task.IsCompleted
                }
            });
        }

        [HttpPatch("{id}/incomplete")]
        public async Task<IActionResult> MarkIncomplete(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var task = await _context.TaskNames
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            task.IsCompleted = false;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Task marked as incomplete",
                task = new
                {
                    task.Id,
                    task.Title,
                    task.IsCompleted
                }
            });
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var totalTasks = await _context.TaskNames
                .CountAsync(t => t.UserId == userId);

            var completedTasks = await _context.TaskNames
                .CountAsync(t => t.UserId == userId && t.IsCompleted);

            var pendingTasks = totalTasks - completedTasks;

            var overdueTasks = await _context.TaskNames
                .CountAsync(t => t.UserId == userId &&
                               !t.IsCompleted &&
                               t.DueDate.HasValue &&
                               t.DueDate.Value < DateTime.Now);

            return Ok(new
            {
                success = true,
                statistics = new
                {
                    total = totalTasks,
                    completed = completedTasks,
                    pending = pendingTasks,
                    overdue = overdueTasks
                }
            });
        }
    }
}
