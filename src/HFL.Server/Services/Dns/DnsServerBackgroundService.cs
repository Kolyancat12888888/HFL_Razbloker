using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HFL.Server.Services.Dns
{
    public class DnsServerBackgroundService : BackgroundService
    {
        private readonly DnsResolverService _resolver;
        private readonly ILogger<DnsServerBackgroundService> _logger;
        private Socket? _socket;

        public DnsServerBackgroundService(DnsResolverService resolver, ILogger<DnsServerBackgroundService> logger)
        {
            _resolver = resolver;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _socket.Bind(new IPEndPoint(IPAddress.Any, 53));

                _logger.LogInformation("🚀 Native DNS Server successfully bound to UDP 0.0.0.0:53");

                byte[] buffer = new byte[4096];
                EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var res = await _socket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEp, stoppingToken);
                    byte[] queryBytes = new byte[res.ReceivedBytes];
                    Array.Copy(buffer, 0, queryBytes, 0, res.ReceivedBytes);
                    var clientEp = res.RemoteEndPoint;
                    string clientIp = (clientEp as IPEndPoint)?.Address.ToString() ?? "";

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            byte[] responseBytes = await _resolver.ProcessDnsQueryAsync(queryBytes, null, clientIp);
                            if (responseBytes.Length > 0 && _socket != null)
                            {
                                await _socket.SendToAsync(responseBytes, SocketFlags.None, clientEp, stoppingToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogTrace("DNS query error from {Client}: {Message}", clientEp, ex.Message);
                        }
                    }, stoppingToken);
                }
            }
            catch (SocketException ex)
            {
                _logger.LogError("❌ COULD NOT BIND UDP PORT 53: {Error}. On Linux, run 'systemctl stop systemd-resolved' or disable DNSStubListener.", ex.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unexpected error in DNS listener");
            }
        }

        public override void Dispose()
        {
            _socket?.Dispose();
            base.Dispose();
        }
    }
}
