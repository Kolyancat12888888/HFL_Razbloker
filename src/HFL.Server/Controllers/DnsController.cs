using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFL.Core.Models;
using HFL.Server.Data;
using HFL.Server.Services.Dns;

namespace HFL.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DnsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly DnsResolverService _resolver;

        public DnsController(AppDbContext db, DnsResolverService resolver)
        {
            _db = db;
            _resolver = resolver;
        }

        [HttpGet("records")]
        public async Task<IActionResult> GetRecords()
        {
            var records = await _db.DnsRecords.OrderBy(r => r.Domain).ToListAsync();
            return Ok(records);
        }

        public record CreateDnsRecordRequest(string Domain, string IpAddress, string? RecordType, int? Ttl, string? Note);

        [HttpPost("records")]
        public async Task<IActionResult> CreateRecord([FromBody] CreateDnsRecordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Domain) || string.IsNullOrWhiteSpace(req.IpAddress))
                return BadRequest(new { error = "Домен и IP адрес обязательны" });

            var record = new DnsRecord
            {
                Domain = req.Domain.Trim().ToLower(),
                IpAddress = req.IpAddress.Trim(),
                RecordType = req.RecordType ?? "A",
                Ttl = req.Ttl ?? 60,
                Note = req.Note,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.DnsRecords.Add(record);
            await _db.SaveChangesAsync();

            return Ok(record);
        }

        [HttpDelete("records/{id}")]
        public async Task<IActionResult> DeleteRecord(int id)
        {
            var record = await _db.DnsRecords.FindAsync(id);
            if (record == null) return NotFound();

            _db.DnsRecords.Remove(record);
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }
}
