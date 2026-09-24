using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HFL.Server.Services.Dns;

namespace HFL.Server.Controllers
{
    [ApiController]
    [Route("dns-query")]
    public class DohController : ControllerBase
    {
        private readonly DnsResolverService _resolver;

        public DohController(DnsResolverService resolver)
        {
            _resolver = resolver;
        }

        private (string? key, string clientIp) GetClientAuth()
        {
            string? key = Request.Query["key"].FirstOrDefault() 
                          ?? Request.Headers["X-License-Key"].FirstOrDefault();
            
            string clientIp = Request.Headers["X-Forwarded-For"].FirstOrDefault() 
                              ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return (key, clientIp);
        }

        [HttpPost]
        public async Task<IActionResult> PostDnsQuery()
        {
            byte[] queryBytes;
            using (var ms = new MemoryStream())
            {
                await Request.Body.CopyToAsync(ms);
                queryBytes = ms.ToArray();
            }

            if (queryBytes.Length < 12)
                return BadRequest("Invalid DNS query length");

            var (key, clientIp) = GetClientAuth();
            byte[] responseBytes = await _resolver.ProcessDnsQueryAsync(queryBytes, key, clientIp);
            
            if (responseBytes == null || responseBytes.Length == 0)
                return StatusCode(504, "DNS upstream timeout");

            return File(responseBytes, "application/dns-message");
        }

        [HttpGet]
        public async Task<IActionResult> GetDnsQuery([FromQuery] string? dns)
        {
            if (string.IsNullOrEmpty(dns))
                return BadRequest("Missing dns query parameter");

            try
            {
                // RFC 8484: Base64URL decoding
                string base64 = dns.Replace('-', '+').Replace('_', '/');
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }

                byte[] queryBytes = Convert.FromBase64String(base64);
                var (key, clientIp) = GetClientAuth();
                byte[] responseBytes = await _resolver.ProcessDnsQueryAsync(queryBytes, key, clientIp);
                
                if (responseBytes == null || responseBytes.Length == 0)
                    return StatusCode(504, "DNS upstream timeout");

                return File(responseBytes, "application/dns-message");
            }
            catch
            {
                return BadRequest("Invalid base64url payload");
            }
        }
    }
}
