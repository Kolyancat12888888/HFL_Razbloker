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

    if (!db.Settings.Any())
    {
        db.Settings.Add(new ServerConfigEntity
        {
            Id = 1,
            DohUpstream = "https://1.1.1.1/dns-query",
            ServerPublicIp = "127.0.0.1",
            XuiVlessUri = "",
            AdminTelegramId = Environment.GetEnvironmentVariable("ADMIN_TELEGRAM_ID") ?? ""
        });
    }

    // Pre-seed default internal test domains
    if (!db.DnsRecords.Any())
    {
        db.DnsRecords.Add(new DnsRecord { Domain = "*.local", IpAddress = "127.0.0.1", IsEnabled = true, Note = "Default .local wildcard" });
        db.DnsRecords.Add(new DnsRecord { Domain = "*.internal", IpAddress = "127.0.0.1", IsEnabled = true, Note = "Default .internal wildcard" });
        db.DnsRecords.Add(new DnsRecord { Domain = "test.local", IpAddress = "127.0.0.1", IsEnabled = true, Note = "Test local domain" });
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
        return Results.Content($"""
        <!DOCTYPE html>
        <html lang="ru">
        <head>
            <meta charset="UTF-8">
            <title>{host} — HFL DNS Test</title>
            <style>
                body {{ background: #0B0F19; color: #F8FAFC; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; }}
                .card {{ background: #131D2F; border: 1px solid #1E293B; border-radius: 12px; padding: 36px; text-align: center; max-width: 500px; box-shadow: 0 10px 30px rgba(0,0,0,0.5); }}
                h1 {{ color: #38BDF8; margin-top: 0; font-size: 24px; }}
                p {{ color: #94A3B8; font-size: 14px; line-height: 1.6; }}
                .badge {{ background: #059669; color: white; padding: 6px 12px; border-radius: 20px; font-weight: bold; font-size: 12px; display: inline-block; margin-bottom: 16px; }}
            </style>
        </head>
        <body>
            <div class="card">
                <div class="badge">🟢 HFL DNS РАБОТАЕТ</div>
                <h1>🎉 Домен {host} успешно открыт!</h1>
                <p>Запрос был прозрачно перехвачен клиентом <b>HFL Razbloker</b> и отрезолвлен на локальный сервер без изменения сетевых настроек Windows.</p>
            </div>
        </body>
        </html>
        """, "text/html; charset=utf-8");
    }

    return Results.Json(new
    {
        service = "HFL Razbloker Enterprise Server",
        version = "1.0.0",
        engine = ".NET 9",
        doh_endpoint = "/dns-query",
        license_endpoint = "/api/v1/license/validate",
        test_domains = new[] { "test.local", "*.local", "*.internal" }
    });
});

app.MapControllers();

app.Run();
