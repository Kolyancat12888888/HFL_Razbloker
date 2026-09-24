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
        private UdpClient? _udpListener;

        public DnsServerBackgroundService(DnsResolverService resolver, ILogger<DnsServerBackgroundService> logger)
        {
            _resolver = resolver;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _udpListener = new UdpClient(new IPEndPoint(IPAddress.Any, 53));
                _logger.LogInformation("Native DNS Server listening on UDP port 53 (Internal & Upstream forwarding active)");

                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = await _udpListener.ReceiveAsync(stoppingToken);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            byte[] responseBytes = await _resolver.ProcessDnsQueryAsync(result.Buffer);
                            if (responseBytes.Length > 0 && _udpListener != null)
                            {
                                await _udpListener.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogTrace("DNS query error from {Client}: {Message}", result.RemoteEndPoint, ex.Message);
                        }
                    }, stoppingToken);
                }
            }
            catch (SocketException ex)
            {
                _logger.LogWarning("Could not bind port 53 (might require Administrator/Root or port is already in use): {Error}. DoH on /dns-query remains fully operational.", ex.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unexpected error in DNS listener");
            }
        }

        public override void Dispose()
        {
            _udpListener?.Dispose();
            base.Dispose();
        }
    }
}
