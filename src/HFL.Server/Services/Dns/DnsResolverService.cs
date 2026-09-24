using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HFL.Core.Models;
using HFL.Server.Data;

namespace HFL.Server.Services.Dns
{
    public class DnsResolverService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DnsResolverService> _logger;
        private readonly HttpClient _httpClient;
        private static readonly IPEndPoint CloudflareEp = new(IPAddress.Parse("1.1.1.1"), 53);
        private static readonly IPEndPoint GoogleEp = new(IPAddress.Parse("8.8.8.8"), 53);

        public DnsResolverService(IServiceProvider serviceProvider, ILogger<DnsResolverService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        }

        public async Task<byte[]> ProcessDnsQueryAsync(byte[] queryBuffer, string? licenseKey = null, string? clientIp = null)
        {
            if (queryBuffer == null || queryBuffer.Length < 12)
                return Array.Empty<byte>();

            // Parse Question Domain Name and QType
            string queriedDomain = ExtractDomainName(queryBuffer, 12, out int questionEndOffset, out ushort qtype);
            queriedDomain = queriedDomain.TrimEnd('.').ToLowerInvariant();

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Check License authorization if key or IP is provided
                if (!string.IsNullOrEmpty(licenseKey) || !string.IsNullOrEmpty(clientIp))
                {
                    var lic = await db.Licenses.FirstOrDefaultAsync(l => 
                        (!string.IsNullOrEmpty(licenseKey) && l.Key == licenseKey.Trim()) ||
                        (!string.IsNullOrEmpty(clientIp) && l.LastSeenIp == clientIp && l.LastSeenIp != "127.0.0.1" && l.LastSeenIp != "unknown"));

                    if (lic != null && !lic.IsActive)
                    {
                        // Blocked client: return 0.0.0.0
                        return BuildResponsePacket(queryBuffer, questionEndOffset, qtype, IPAddress.Parse("0.0.0.0"), 5);
                    }
                }

                if (!string.IsNullOrEmpty(queriedDomain))
                {
                    var settings = await db.Settings.FirstOrDefaultAsync() ?? new ServerConfigEntity();
                    var records = await db.DnsRecords.Where(r => r.IsEnabled).ToListAsync();

                    // Check for local domain match (exact or *.local / *.internal)
                    var matchedRecord = records.FirstOrDefault(r =>
                    {
                        string recDom = r.Domain.TrimEnd('.').ToLowerInvariant();
                        if (recDom == queriedDomain) return true;
                        if (recDom.StartsWith("*.") && (queriedDomain.EndsWith(recDom.Substring(1)) || queriedDomain == recDom.Substring(2))) return true;
                        return false;
                    });

                    // Auto-resolve any .local or .internal domain if not explicitly listed
                    if (matchedRecord != null || queriedDomain.EndsWith(".local") || queriedDomain.EndsWith(".internal"))
                    {
                        string targetIpStr = matchedRecord?.IpAddress ?? settings.ServerPublicIp ?? "31.77.8.9";
                        if (!IPAddress.TryParse(targetIpStr, out var targetIp))
                        {
                            targetIp = IPAddress.Parse("31.77.8.9");
                        }

                        int ttl = (matchedRecord != null && matchedRecord.Ttl > 0) ? matchedRecord.Ttl : 60;
                        return BuildResponsePacket(queryBuffer, questionEndOffset, qtype, targetIp, ttl);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace("DB query error in DNS resolver: {Error}", ex.Message);
            }

            // External domain resolution: fast concurrent UDP queries to 1.1.1.1 and 8.8.8.8
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var t1 = QueryUdpDnsAsync(queryBuffer, CloudflareEp, cts.Token);
                var t2 = QueryUdpDnsAsync(queryBuffer, GoogleEp, cts.Token);

                var completed = await Task.WhenAny(t1, t2);
                byte[] res = await completed;
                if (res != null && res.Length > 0)
                {
                    return res;
                }

                // If first completed task returned empty, check the other
                var other = (completed == t1) ? t2 : t1;
                byte[] otherRes = await other;
                if (otherRes != null && otherRes.Length > 0)
                {
                    return otherRes;
                }
            }
            catch { }

            return Array.Empty<byte>();
        }

        private static async Task<byte[]> QueryUdpDnsAsync(byte[] queryBuffer, IPEndPoint target, CancellationToken token)
        {
            try
            {
                using var udpClient = new UdpClient();
                await udpClient.SendAsync(queryBuffer, queryBuffer.Length, target);
                var res = await udpClient.ReceiveAsync(token);
                return res.Buffer;
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private static string ExtractDomainName(byte[] buffer, int offset, out int nextOffset, out ushort qtype)
        {
            var sb = new StringBuilder();
            nextOffset = offset;
            qtype = 1; // default A

            while (nextOffset < buffer.Length)
            {
                byte len = buffer[nextOffset++];
                if (len == 0) break;

                // Pointer
                if ((len & 0xC0) == 0xC0)
                {
                    nextOffset++; // skip second byte of pointer
                    break;
                }

                if (nextOffset + len > buffer.Length) break;

                if (sb.Length > 0) sb.Append('.');
                sb.Append(Encoding.ASCII.GetString(buffer, nextOffset, len));
                nextOffset += len;
            }

            if (nextOffset + 4 <= buffer.Length)
            {
                qtype = (ushort)((buffer[nextOffset] << 8) | buffer[nextOffset + 1]);
                nextOffset += 4; // Skip QTYPE (2 bytes) and QCLASS (2 bytes)
            }

            return sb.ToString().ToLowerInvariant();
        }

        private static byte[] BuildResponsePacket(byte[] queryBuffer, int questionLength, ushort qtype, IPAddress ip, int ttl)
        {
            // If query is for AAAA (IPv6), return NOERROR with 0 answers so resolver immediately uses A record
            if (qtype == 28) // AAAA
            {
                byte[] noAnswers = new byte[questionLength];
                Array.Copy(queryBuffer, 0, noAnswers, 0, questionLength);
                noAnswers[2] = 0x85; // QR=1, AA=1, RD=1
                noAnswers[3] = 0x80; // RA=1, RCODE=0
                noAnswers[6] = 0x00; // ANCOUNT = 0
                noAnswers[7] = 0x00;
                return noAnswers;
            }

            byte[] ipBytes = ip.GetAddressBytes();
            if (ipBytes.Length != 4)
                ipBytes = new byte[] { 31, 77, 8, 9 };

            int length = Math.Min(questionLength, queryBuffer.Length);
            byte[] response = new byte[length + 16];

            // Copy Header & Question
            Array.Copy(queryBuffer, 0, response, 0, length);

            // Set Flags: Standard query response, Authoritative Answer, Recursion Desired & Available (0x8580)
            response[2] = 0x85;
            response[3] = 0x80;

            // Set Questions = 1, Answers = 1, Authority = 0, Additional = 0
            response[4] = 0x00;
            response[5] = 0x01;
            response[6] = 0x00;
            response[7] = 0x01;
            response[8] = 0x00;
            response[9] = 0x00;
            response[10] = 0x00;
            response[11] = 0x00;

            int offset = length;

            // Answer Name Pointer to Question (0xC00C)
            response[offset++] = 0xC0;
            response[offset++] = 0x0C;

            // Type A (0x0001)
            response[offset++] = 0x00;
            response[offset++] = 0x01;

            // Class IN (0x0001)
            response[offset++] = 0x00;
            response[offset++] = 0x01;

            // TTL (4 bytes)
            response[offset++] = (byte)((ttl >> 24) & 0xFF);
            response[offset++] = (byte)((ttl >> 16) & 0xFF);
            response[offset++] = (byte)((ttl >> 8) & 0xFF);
            response[offset++] = (byte)(ttl & 0xFF);

            // Data Length (4 bytes for IPv4)
            response[offset++] = 0x00;
            response[offset++] = 0x04;

            // IP Address (4 bytes)
            Array.Copy(ipBytes, 0, response, offset, 4);

            return response;
        }
    }
}
