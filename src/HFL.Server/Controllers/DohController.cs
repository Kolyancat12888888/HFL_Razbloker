using System;
using System.IO;
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

        [HttpPost]
        [Consumes("application/dns-message")]
        [Produces("application/dns-message")]
        public async Task<IActionResult> PostDnsQuery()
        {
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);
            byte[] queryBytes = ms.ToArray();

            if (queryBytes.Length == 0)
                return BadRequest();

            byte[] responseBytes = await _resolver.ProcessDnsQueryAsync(queryBytes);
            return File(responseBytes, "application/dns-message");
        }

        [HttpGet]
        [Produces("application/dns-message")]
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
                byte[] responseBytes = await _resolver.ProcessDnsQueryAsync(queryBytes);
                return File(responseBytes, "application/dns-message");
            }
            catch
            {
                return BadRequest("Invalid base64url payload");
            }
        }
    }
}
