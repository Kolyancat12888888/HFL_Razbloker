using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HFL.Core.Models;
using HFL.Server.Data;
using HFL.Server.Services.Dns;
using HFL.Server.Services.Telegram;

var builder = WebApplication.CreateBuilder(args);

string dataDir = Path.Combine(AppContext.BaseDirectory, "data");
if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
string dbPath = Path.Combine(dataDir, "hfl_enterprise.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddSingleton<DnsResolverService>();
builder.Services.AddHostedService<DnsServerBackgroundService>();
builder.Services.AddHostedService<TelegramBotService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Auto-migrate schema changes in SQLite
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Licenses ADD COLUMN LastSeenIp TEXT NULL;"); } catch { }
    try
    {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS PanelUsers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                Role TEXT NOT NULL,
                DiskQuota TEXT NOT NULL,
                MaxSites INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                LastLoginAt TEXT NULL
            );");
    }
    catch { }

    var setting = db.Settings.FirstOrDefault();
    if (setting == null)
    {
        setting = new ServerConfigEntity
        {
            Id = 1,
            DohUpstream = "https://1.1.1.1/dns-query",
            ServerPublicIp = "31.77.8.9",
            XuiVlessUri = "",
            AdminTelegramId = "6014501462"
        };
        db.Settings.Add(setting);
    }
    else
    {
        setting.ServerPublicIp = "31.77.8.9";
        setting.AdminTelegramId = "6014501462";
    }

    // Pre-seed or update default internal domains to point to server IP 31.77.8.9
    var existingTest = db.DnsRecords.FirstOrDefault(r => r.Domain == "test.local");
    if (existingTest == null)
    {
        db.DnsRecords.Add(new DnsRecord { Domain = "*.local", IpAddress = "31.77.8.9", IsEnabled = true, Note = "Default .local wildcard" });
        db.DnsRecords.Add(new DnsRecord { Domain = "*.internal", IpAddress = "31.77.8.9", IsEnabled = true, Note = "Default .internal wildcard" });
        db.DnsRecords.Add(new DnsRecord { Domain = "test.local", IpAddress = "31.77.8.9", IsEnabled = true, Note = "Test local domain" });
    }
    else
    {
        existingTest.IpAddress = "31.77.8.9";
        var localWild = db.DnsRecords.FirstOrDefault(r => r.Domain == "*.local");
        if (localWild != null) localWild.IpAddress = "31.77.8.9";
        var internalWild = db.DnsRecords.FirstOrDefault(r => r.Domain == "*.internal");
        if (internalWild != null) internalWild.IpAddress = "31.77.8.9";
    }

    db.SaveChanges();
}

app.UseCors();
app.UseRouting();

// Fallback HTML page for any .local / .internal test request
app.MapGet("/", (HttpContext context) =>
{
    string host = context.Request.Host.Host;
    if (host.EndsWith(".local") || host.EndsWith(".internal"))
    {
        return Results.Content($$"""
        <!DOCTYPE html>
        <html lang="ru">
        <head>
            <meta charset="UTF-8">
            <title>{{host}} — HFL DNS Test</title>
            <style>
                body { background: #0B0F19; color: #F8FAFC; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; }
                .card { background: #131D2F; border: 1px solid #1E293B; border-radius: 12px; padding: 36px; text-align: center; max-width: 500px; box-shadow: 0 10px 30px rgba(0,0,0,0.5); }
                h1 { color: #38BDF8; margin-top: 0; font-size: 24px; }
                p { color: #94A3B8; font-size: 14px; line-height: 1.6; }
                .badge { background: #059669; color: white; padding: 6px 12px; border-radius: 20px; font-weight: bold; font-size: 12px; display: inline-block; margin-bottom: 16px; }
            </style>
        </head>
        <body>
            <div class="card">
                <div class="badge">🟢 HFL DNS РАБОТАЕТ</div>
                <h1>🎉 Домен {{host}} успешно открыт!</h1>
                <p>Запрос был прозрачно перехвачен клиентом <b>HFL Razbloker</b> и отрезолвлен на сервер <b>31.77.8.9</b> без изменения сетевых настроек Windows.</p>
            </div>
        </body>
        </html>
        """, "text/html; charset=utf-8");
    }

    return Results.Json(new
    {
        service = "HFL Razbloker Enterprise Server",
        version = "1.0.0",
        server_ip = "31.77.8.9",
        doh_endpoint = "/dns-query",
        license_endpoint = "/api/v1/license/validate",
        test_domains = new[] { "test.local", "*.local", "*.internal" }
    });
});

app.MapControllers();

app.Run();
