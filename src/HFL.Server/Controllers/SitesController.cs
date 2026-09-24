using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HFL.Core.Models;
using HFL.Server.Data;

namespace HFL.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SitesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SitesController(AppDbContext db)
        {
            _db = db;
        }

        public class SiteDto
        {
            public string Domain { get; set; } = string.Empty;
            public string EnvType { get; set; } = "PHP";
            public string PhpVersion { get; set; } = "8.3";
            public string DocRoot { get; set; } = "/var/www";
            public string SslType { get; set; } = "HFL SAN SSL";
            public bool IsActive { get; set; } = true;
        }

        [HttpGet]
        public IActionResult GetSites()
        {
            var list = new List<SiteDto>();

            if (OperatingSystem.IsLinux() && Directory.Exists("/etc/nginx/sites-enabled"))
            {
                try
                {
                    foreach (var file in Directory.GetFiles("/etc/nginx/sites-enabled"))
                    {
                        string content = System.IO.File.ReadAllText(file);
                        string domain = Path.GetFileName(file).Replace(".conf", "");
                        string docRoot = "/var/www/" + domain;
                        string envType = content.Contains("fastcgi_pass") ? "PHP" : (content.Contains("proxy_pass") ? "Proxy" : "Static");

                        list.Add(new SiteDto
                        {
                            Domain = domain,
                            DocRoot = docRoot,
                            EnvType = envType,
                            PhpVersion = envType == "PHP" ? "8.3" : "N/A",
                            SslType = content.Contains("ssl_certificate") ? "HFL SAN SSL" : "None"
                        });
                    }
                }
                catch { }
            }

            if (list.Count == 0)
            {
                list.Add(new SiteDto { Domain = "hfl-control-panel.internal", DocRoot = "/var/www/hfl-panel", EnvType = "Vue/SPA", PhpVersion = "SPA", SslType = "HFL SAN SSL" });
                list.Add(new SiteDto { Domain = "test.local", DocRoot = "/var/www/hfl-local", EnvType = "PHP", PhpVersion = "8.3", SslType = "HFL SAN SSL" });
                list.Add(new SiteDto { Domain = "cs2.hfl-nodes.pro", DocRoot = "/var/www/cs2panel", EnvType = "PHP", PhpVersion = "8.2", SslType = "Let's Encrypt" });
            }

            return Ok(list);
        }

        public record CreateSiteRequest(string Domain, string? EnvType, string? PhpVersion, string? DocRoot);

        [HttpPost]
        public async Task<IActionResult> CreateSite([FromBody] CreateSiteRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Domain))
                return BadRequest(new { error = "Домен обязателен" });

            string domain = req.Domain.Trim().ToLower();
            string docRoot = string.IsNullOrWhiteSpace(req.DocRoot) ? $"/var/www/{domain}" : req.DocRoot;

            if (OperatingSystem.IsLinux())
            {
                try
                {
                    if (!Directory.Exists(docRoot)) Directory.CreateDirectory(docRoot);

                    string nginxConfig = $$"""
                    server {
                        listen 80;
                        listen 31.77.8.9:80;
                        listen 443 ssl;
                        listen 31.77.8.9:443 ssl;
                        server_name {{domain}};
                        
                        ssl_certificate /etc/ssl/certs/ssl-cert-snakeoil.pem;
                        ssl_certificate_key /etc/ssl/private/ssl-cert-snakeoil.key;

                        root {{docRoot}};
                        index index.php index.html;

                        location / {
                            try_files $uri $uri/ /index.php?$query_string;
                        }

                        location ~ \.php$ {
                            include snippets/fastcgi-php.conf;
                            fastcgi_pass unix:/run/php/php8.3-fpm.sock;
                        }
                    }
                    """;

                    string availPath = $"/etc/nginx/sites-available/{domain}.conf";
                    string enabledPath = $"/etc/nginx/sites-enabled/{domain}.conf";

                    if (Directory.Exists("/etc/nginx/sites-available"))
                    {
                        await System.IO.File.WriteAllTextAsync(availPath, nginxConfig);
                        if (!System.IO.File.Exists(enabledPath))
                        {
                            var psi = new ProcessStartInfo("ln", $"-sf {availPath} {enabledPath}") { UseShellExecute = false };
                            Process.Start(psi)?.WaitForExit();
                        }

                        var reloadPsi = new ProcessStartInfo("systemctl", "reload nginx") { UseShellExecute = false };
                        Process.Start(reloadPsi)?.WaitForExit();
                    }
                }
                catch { }
            }

            // Also auto-add DNS record
            var existing = await _db.DnsRecords.FirstOrDefaultAsync(r => r.Domain == domain);
            if (existing == null)
            {
                _db.DnsRecords.Add(new DnsRecord
                {
                    Domain = domain,
                    IpAddress = "31.77.8.9",
                    IsEnabled = true,
                    Note = "Auto-created site"
                });
                await _db.SaveChangesAsync();
            }

            return Ok(new { success = true, domain, docRoot });
        }
    }
}
