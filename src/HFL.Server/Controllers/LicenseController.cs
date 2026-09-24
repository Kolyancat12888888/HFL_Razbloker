using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFL.Core.Models;
using HFL.Server.Data;

namespace HFL.Server.Controllers
{
    [ApiController]
    [Route("api/v1/license")]
    public class LicenseController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LicenseController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] ValidateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Key) || string.IsNullOrWhiteSpace(req.Hwid))
            {
                return BadRequest(new ValidateResponse { Valid = false, Status = "invalid_payload", Message = "Ключ и HWID обязательны" });
            }

            var lic = await _db.Licenses.FirstOrDefaultAsync(l => l.Key == req.Key.Trim());
            if (lic == null)
            {
                return Ok(new ValidateResponse { Valid = false, Status = "not_found", Message = "Ключ лицензии не существует" });
            }

            if (!lic.IsActive)
            {
                return Ok(new ValidateResponse { Valid = false, Status = "revoked", Message = "Ключ заблокирован администратором" });
            }

            var now = DateTime.UtcNow;

            // First activation
            if (lic.ActivatedAt == null)
            {
                lic.ActivatedAt = now;
                lic.Hwid = req.Hwid;
                if (lic.DurationDays != -1)
                {
                    lic.ExpiresAt = now.AddDays(lic.DurationDays);
                }
            }
            else
            {
                // Check HWID
                if (!string.IsNullOrEmpty(lic.Hwid) && lic.Hwid != req.Hwid)
                {
                    return Ok(new ValidateResponse
                    {
                        Valid = false,
                        Status = "hwid_mismatch",
                        Message = "Ключ привязан к другому компьютеру. Запросите сброс HWID у администратора."
                    });
                }
                else if (string.IsNullOrEmpty(lic.Hwid))
                {
                    lic.Hwid = req.Hwid;
                }

                // Check Expiration
                if (lic.ExpiresAt != null && lic.ExpiresAt < now)
                {
                    return Ok(new ValidateResponse
                    {
                        Valid = false,
                        Status = "expired",
                        Message = $"Срок действия ключа истек {lic.ExpiresAt:dd.MM.yyyy}"
                    });
                }
            }

            lic.LastSeenAt = now;
            if (!string.IsNullOrEmpty(req.AppVersion))
            {
                lic.AppVersion = req.AppVersion;
            }

            await _db.SaveChangesAsync();

            var settings = await _db.Settings.FirstOrDefaultAsync() ?? new ServerConfigEntity();

            int daysLeft = -1;
            if (lic.ExpiresAt != null)
            {
                daysLeft = Math.Max(0, (int)(lic.ExpiresAt.Value - now).TotalDays);
            }

            return Ok(new ValidateResponse
            {
                Valid = true,
                Status = "active",
                Key = lic.Key,
                ExpiresAt = lic.ExpiresAt?.ToString("dd.MM.yyyy HH:mm") ?? "Lifetime",
                DaysLeft = daysLeft,
                ServerConfig = new ClientServerConfig
                {
                    DohUrl = $"{Request.Scheme}://{Request.Host}/dns-query",
                    VlessUri = settings.XuiVlessUri,
                    ZapretStrategies = new[]
                    {
                        "--wf-tcp=80,443 --wf-udp=443,50000-50050 --dpi-desync=fake,split2 --dpi-desync-autottl=2 --dpi-desync-fooling=md5sig",
                        "--wf-tcp=80,443 --wf-udp=443,50000-50050 --dpi-desync=disorder2 --dpi-desync-split-pos=1 --dpi-desync-fooling=badseq",
                        "--wf-tcp=80,443 --wf-udp=443,50000-50050 --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fooling=badsum"
                    }
                }
            });
        }

        [HttpPost("ping")]
        public async Task<IActionResult> Ping([FromBody] ValidateRequest req)
        {
            var lic = await _db.Licenses.FirstOrDefaultAsync(l => l.Key == req.Key.Trim() && l.Hwid == req.Hwid);
            if (lic != null && lic.IsActive)
            {
                lic.LastSeenAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(new { status = "ok", valid = true });
            }
            return Ok(new { status = "invalid", valid = false });
        }
    }
}
