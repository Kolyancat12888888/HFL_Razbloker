using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        db.SaveChanges();
    }
}

app.UseCors();
app.UseRouting();

app.MapGet("/", () => Results.Json(new
{
    service = "HFL Razbloker Enterprise Server",
    version = "1.0.0",
    engine = ".NET 9",
    doh_endpoint = "/dns-query",
    license_endpoint = "/api/v1/license/validate"
}));

app.MapControllers();

app.Run();
