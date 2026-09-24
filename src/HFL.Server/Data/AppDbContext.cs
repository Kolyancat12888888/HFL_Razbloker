using System;
using Microsoft.EntityFrameworkCore;
using HFL.Core.Models;

namespace HFL.Server.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<LicenseInfo> Licenses => Set<LicenseInfo>();
        public DbSet<DnsRecord> DnsRecords => Set<DnsRecord>();
        public DbSet<ServerConfigEntity> Settings => Set<ServerConfigEntity>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LicenseInfo>().HasIndex(l => l.Key).IsUnique();
            modelBuilder.Entity<LicenseInfo>().HasIndex(l => l.Hwid);
            modelBuilder.Entity<DnsRecord>().HasIndex(r => r.Domain);

            base.OnModelCreating(modelBuilder);
        }
    }

    public class ServerConfigEntity
    {
        public int Id { get; set; } = 1;
        public string DohUpstream { get; set; } = "https://1.1.1.1/dns-query";
        public string ServerPublicIp { get; set; } = "127.0.0.1";
        public string XuiVlessUri { get; set; } = "";
        public string AdminTelegramId { get; set; } = "";
        public string ZapretStrategiesJson { get; set; } = "[]";
    }
}
