using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskManger.Data;
using TaskManger.Models;

namespace TaskManger.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly TaskDb _context;
        private readonly IConfiguration _configuration;  // ← ADD THIS!

        // ← FIX: Add IConfiguration to constructor
        public ValuesController(TaskDb context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;  // ← ADD THIS!
        }

        // Request Models
        public class RegisterRequest
        {
            public string Name { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class LoginModel
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        // REGISTER ENDPOINT
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest(new { message = "Username already exists" });

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest(new { message = "Email already exists" });

            // Hash password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create new user
            var user = new Users
            {
                Name = request.Name,
                Email = request.Email,
                Password = hashedPassword,
                Username = request.Username,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully", userid = user.Id });
        }

        // LOGIN ENDPOINT
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // Find user (FIX: Changed _db to _context)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null)
                return Unauthorized(new { message = "Invalid credentials" });

            // Check password
            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                return Unauthorized(new { message = "Invalid credentials" });

            // Create token
            var token = GenerateToken(user);

            return Ok(new
            {
                message = "Login successful",
                token = token,
                userId = user.Id,
                username = user.Username
            });
        }

        // TOKEN GENERATION (FIX: Using _configuration instead of _config)
        private string GenerateToken(Users user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}