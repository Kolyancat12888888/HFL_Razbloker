using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFL.Core.Models;
using HFL.Server.Data;

namespace HFL.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LicensesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LicensesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllLicenses()
        {
            var list = await _db.Licenses
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    id = l.Id,
                    key = l.Key,
                    duration_days = l.DurationDays,
                    is_active = l.IsActive,
                    hwid = l.Hwid ?? "Не привязан",
                    ip = l.LastSeenIp ?? "—",
                    created_at = l.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    activated_at = l.ActivatedAt.HasValue ? l.ActivatedAt.Value.ToString("yyyy-MM-dd HH:mm") : "Не активирован",
                    expires_at = l.ExpiresAt.HasValue ? l.ExpiresAt.Value.ToString("yyyy-MM-dd HH:mm") : (l.DurationDays == -1 ? "Бессрочно" : "—"),
                    last_seen = l.LastSeenAt.HasValue ? l.LastSeenAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Никогда",
                    note = l.Note ?? "",
                    app_version = l.AppVersion ?? "—"
                })
                .ToListAsync();

            return Ok(list);
        }

        public record CreateLicenseRequest(int? DurationDays, string? Note);

        [HttpPost]
        public async Task<IActionResult> CreateLicense([FromBody] CreateLicenseRequest req)
        {
            string key = "HFL-" + GenerateRandomKey();
            var lic = new LicenseInfo
            {
                Key = key,
                DurationDays = req.DurationDays ?? -1,
                Note = req.Note,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Licenses.Add(lic);
            await _db.SaveChangesAsync();

            return Ok(lic);
        }

        [HttpPost("{id}/toggle")]
        public async Task<IActionResult> ToggleLicenseStatus(int id)
        {
            var lic = await _db.Licenses.FindAsync(id);
            if (lic == null) return NotFound(new { error = "Лицензия не найдена" });

            lic.IsActive = !lic.IsActive;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, is_active = lic.IsActive, key = lic.Key });
        }

        [HttpPost("{id}/reset-hwid")]
        public async Task<IActionResult> ResetHwid(int id)
        {
            var lic = await _db.Licenses.FindAsync(id);
            if (lic == null) return NotFound(new { error = "Лицензия не найдена" });

            lic.Hwid = null;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Привязка HWID успешно сброшена" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLicense(int id)
        {
            var lic = await _db.Licenses.FindAsync(id);
            if (lic == null) return NotFound(new { error = "Лицензия не найдена" });

            _db.Licenses.Remove(lic);
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private static string GenerateRandomKey()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                if (i > 0 && i % 4 == 0) sb.Append('-');
                sb.Append(chars[bytes[i] % chars.Length]);
            }
            return sb.ToString();
        }
    }
}
