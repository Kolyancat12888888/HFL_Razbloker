using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFL.Core.Models;
using HFL.Server.Data;

namespace HFL.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuthController(AppDbContext db)
        {
            _db = db;
            SeedDefaultUsers();
        }

        private void SeedDefaultUsers()
        {
            if (!_db.PanelUsers.Any())
            {
                _db.PanelUsers.Add(new PanelUser
                {
                    Username = "root",
                    PasswordHash = HashPassword("root"),
                    Role = "SuperAdmin",
                    DiskQuota = "Unlimited",
                    MaxSites = 999,
                    CreatedAt = DateTime.UtcNow
                });
                _db.PanelUsers.Add(new PanelUser
                {
                    Username = "admin",
                    PasswordHash = HashPassword("admin"),
                    Role = "Admin",
                    DiskQuota = "100GB",
                    MaxSites = 50,
                    CreatedAt = DateTime.UtcNow
                });
                _db.PanelUsers.Add(new PanelUser
                {
                    Username = "client",
                    PasswordHash = HashPassword("client"),
                    Role = "Client",
                    DiskQuota = "10GB",
                    MaxSites = 5,
                    CreatedAt = DateTime.UtcNow
                });
                _db.SaveChanges();
            }
        }

        public record LoginRequest(string Username, string Password);
        public record CreateUserRequest(string Username, string Password, string? Role, string? DiskQuota, int? MaxSites);

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "Имя пользователя и пароль обязательны" });

            string hash = HashPassword(req.Password);
            var user = await _db.PanelUsers.FirstOrDefaultAsync(u => u.Username.ToLower() == req.Username.ToLower());

            if (user == null || (user.PasswordHash != hash && user.PasswordHash != req.Password))
            {
                return Unauthorized(new { error = "Неверное имя пользователя или пароль" });
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Generate simple token
            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user.Username}:{user.Role}:{DateTime.UtcNow.Ticks}"));

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    role = user.Role,
                    diskQuota = user.DiskQuota,
                    maxSites = user.MaxSites
                }
            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _db.PanelUsers.Select(u => new
            {
                id = u.Id,
                username = u.Username,
                role = u.Role,
                quota = new { disk = u.DiskQuota, sites = u.MaxSites },
                created_at = u.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                last_login = u.LastLoginAt.HasValue ? u.LastLoginAt.Value.ToString("yyyy-MM-dd HH:mm") : "Никогда"
            }).ToListAsync();

            return Ok(users);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { error = "Имя пользователя и пароль обязательны" });

            bool exists = await _db.PanelUsers.AnyAsync(u => u.Username.ToLower() == req.Username.ToLower());
            if (exists) return Conflict(new { error = "Пользователь с таким именем уже существует" });

            var user = new PanelUser
            {
                Username = req.Username.Trim(),
                PasswordHash = HashPassword(req.Password),
                Role = req.Role ?? "Client",
                DiskQuota = req.DiskQuota ?? "10GB",
                MaxSites = req.MaxSites ?? 5,
                CreatedAt = DateTime.UtcNow
            };

            _db.PanelUsers.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, user = new { id = user.Id, username = user.Username, role = user.Role } });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _db.PanelUsers.FindAsync(id);
            if (user == null) return NotFound();
            if (user.Username == "root") return BadRequest(new { error = "Нельзя удалить root пользователя" });

            _db.PanelUsers.Remove(user);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "HFL_SALT_ENTERPRISE_2026"));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
