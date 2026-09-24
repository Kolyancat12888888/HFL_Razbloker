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
using HFL.Server.Data;

namespace HFL.Server.Services.Dns
{
    public class DnsResolverService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DnsResolverService> _logger;
        private readonly HttpClient _httpClient;

        public DnsResolverService(IServiceProvider serviceProvider, ILogger<DnsResolverService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
        }

        public async Task<byte[]> ProcessDnsQueryAsync(byte[] queryBuffer)
        {
            if (queryBuffer == null || queryBuffer.Length < 12)
                return Array.Empty<byte>();

            // Parse Question Domain Name from standard RFC 1035 DNS packet
            string queriedDomain = ExtractDomainName(queryBuffer, 12, out int questionEndOffset);

            if (!string.IsNullOrEmpty(queriedDomain))
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var settings = await db.Settings.FirstOrDefaultAsync() ?? new ServerConfigEntity();
                var records = await db.DnsRecords.Where(r => r.IsEnabled).ToListAsync();

                // Check for local domain match (exact or *.local / *.internal)
                var matchedRecord = records.FirstOrDefault(r =>
                {
                    string recDom = r.Domain.TrimEnd('.').ToLowerInvariant();
                    if (recDom == queriedDomain) return true;
                    if (recDom.StartsWith("*.") && queriedDomain.EndsWith(recDom.Substring(1))) return true;
                    return false;
                });

                if (matchedRecord != null)
                {
                    string targetIpStr = string.IsNullOrWhiteSpace(matchedRecord.IpAddress) 
                        ? settings.ServerPublicIp 
                        : matchedRecord.IpAddress;

                    if (!IPAddress.TryParse(targetIpStr, out var targetIp))
                    {
                        targetIp = IPAddress.Loopback;
                    }

                    return BuildAResponsePacket(queryBuffer, questionEndOffset, targetIp, matchedRecord.Ttl > 0 ? matchedRecord.Ttl : 60);
                }

                // If not an internal domain, forward directly to Cloudflare DoH (1.1.1.1)
                try
                {
                    string upstream = string.IsNullOrWhiteSpace(settings.DohUpstream) 
                        ? "https://1.1.1.1/dns-query" 
                        : settings.DohUpstream;

                    using var request = new HttpRequestMessage(HttpMethod.Post, upstream);
                    request.Content = new ByteArrayContent(queryBuffer);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/dns-message");
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-message"));

                    var httpResponse = await _httpClient.SendAsync(request);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        return await httpResponse.Content.ReadAsByteArrayAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogTrace("DoH upstream forward failed: {Error}", ex.Message);
                }
            }

            // Fallback: standard UDP forward to 1.1.1.1:53
            try
            {
                using var udpClient = new UdpClient();
                await udpClient.SendAsync(queryBuffer, queryBuffer.Length, new IPEndPoint(IPAddress.Parse("1.1.1.1"), 53));
                
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var receiveTask = udpClient.ReceiveAsync(cts.Token);
                var res = await receiveTask;
                return res.Buffer;
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private static string ExtractDomainName(byte[] buffer, int offset, out int nextOffset)
        {
            var sb = new StringBuilder();
            nextOffset = offset;

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

            // Skip QTYPE (2 bytes) and QCLASS (2 bytes)
            nextOffset += 4;
            return sb.ToString().ToLowerInvariant();
        }

        private static byte[] BuildAResponsePacket(byte[] queryBuffer, int questionEndOffset, IPAddress ip, int ttl)
        {
            byte[] ipBytes = ip.GetAddressBytes();
            if (ipBytes.Length != 4) // IPv4 only for A record
                ipBytes = new byte[] { 127, 0, 0, 1 };

            int questionLength = Math.Min(questionEndOffset, queryBuffer.Length);
            byte[] response = new byte[questionLength + 16];

            // Copy Header & Question
            Array.Copy(queryBuffer, 0, response, 0, questionLength);

            // Set Flags: Standard query response, No error (0x8180)
            response[2] = 0x81;
            response[3] = 0x80;

            // Set Answer Count = 1
            response[6] = 0x00;
            response[7] = 0x01;

            int offset = questionLength;

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
