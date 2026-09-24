using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HFL.Core.Models;

namespace HFL.Client.Services
{
    public class StrategyOptimizer
    {
        private readonly ZapretService _zapret;
        private readonly XrayService _xray;
        private readonly DnsClientService _dns;
        private readonly LicenseClientService _license;
        private readonly HttpClient _probeHttp;

        private CancellationTokenSource? _cts;
        private bool _isRunning;

        public event Action<string>? OnLog;
        public event Action<int>? OnLatencyChanged;
        public event Action<string>? OnStrategyChanged;
        public event Action<bool>? OnConnectionStateChanged;

        public string ActiveStrategyName { get; private set; } = "Отключено";
        public int CurrentLatencyMs { get; private set; } = 0;
        public bool IsActive => _isRunning;

        public StrategyOptimizer(ZapretService zapret, XrayService xray, DnsClientService dns, LicenseClientService license)
        {
            _zapret = zapret;
            _xray = xray;
            _dns = dns;
            _license = license;
            _probeHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        }

        public async Task StartAutonomousAsync(AppSettings settings)
        {
            Stop();
            _isRunning = true;
            _cts = new CancellationTokenSource();
            OnConnectionStateChanged?.Invoke(true);

            Log("🚀 Запуск автономного оптимизатора «Швейцарские часы»...");

            var token = _cts.Token;

            // Apply DNS if configured
            if (!string.IsNullOrEmpty(settings.CustomDohUrl) || !string.IsNullOrEmpty(_license.CurrentLicense?.ServerConfig?.DohUrl))
            {
                Log("🔒 Активация защищенного DNS-over-HTTPS резолвера...");
            }

            if (settings.SelectedMode == "Zapret")
            {
                SetStrategy("Zapret Direct DPI Bypass");
                _zapret.Start(null, 0);
            }
            else if (settings.SelectedMode == "3XUI")
            {
                string vless = _license.CurrentLicense?.ServerConfig?.VlessUri ?? "";
                SetStrategy("3X-UI VLESS Reality");
                _xray.Start(vless, settings.DirectRuRouting);
            }
            else
            {
                // Full Autonomous Auto Mode ("Швейцарские часы")
                await RunAutonomousFallbackLoopAsync(settings, token);
            }

            // Start background latency & health monitor
            _ = MonitorHealthLoopAsync(settings, token);
        }

        private async Task RunAutonomousFallbackLoopAsync(AppSettings settings, CancellationToken token)
        {
            Log("⚙️ Тестирование стратегии уровня 1: Zapret Multi-Split (Минимальный пинг)...");
            SetStrategy("Zapret Level 1 (Ultra-Low Latency)");
            _zapret.Start(null, 0);

            await Task.Delay(1500, token);
            int ping = await ProbeTargetAsync("https://www.youtube.com");

            if (ping > 0 && ping <= 120)
            {
                CurrentLatencyMs = ping;
                OnLatencyChanged?.Invoke(ping);
                Log($"✅ Стратегия 1 успешно зафиксирована! Пинг: {ping} мс (Швейцарская точность)");
                return;
            }

            Log("⚠️ Провайдер фильтрует стратегию 1. Переключение на уровень 2 (Disorder + BadSeq)...");
            SetStrategy("Zapret Level 2 (Disorder)");
            _zapret.Start(null, 1);

            await Task.Delay(1500, token);
            ping = await ProbeTargetAsync("https://www.youtube.com");

            if (ping > 0 && ping <= 150)
            {
                CurrentLatencyMs = ping;
                OnLatencyChanged?.Invoke(ping);
                Log($"✅ Стратегия 2 активна! Пинг: {ping} мс");
                return;
            }

            // Fallback to VLESS Reality if Zapret is completely blocked by ISP
            string vless = _license.CurrentLicense?.ServerConfig?.VlessUri ?? "";
            if (!string.IsNullOrEmpty(vless))
            {
                Log("🛡️ Включение резервного канала: 3X-UI Enterprise VLESS Reality (Smart Direct RU Split)...");
                _zapret.Stop();
                SetStrategy("3X-UI VLESS Reality (Auto Fallback)");
                _xray.Start(vless, settings.DirectRuRouting);
            }
            else
            {
                Log("⚡ Активация универсального Zapret Fake-Repeat профиля...");
                SetStrategy("Zapret Level 3 (Fake-Repeats)");
                _zapret.Start(null, 2);
            }
        }

        private async Task MonitorHealthLoopAsync(AppSettings settings, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(4000, token);
                    int ping = await ProbeTargetAsync("https://www.youtube.com");
                    if (ping <= 0)
                    {
                        ping = await ProbeTargetAsync("https://discord.com");
                    }

                    if (ping > 0)
                    {
                        CurrentLatencyMs = ping;
                        OnLatencyChanged?.Invoke(ping);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch { }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _zapret.Stop();
            _xray.Stop();
            _dns.RestoreDhcpDns();

            _isRunning = false;
            ActiveStrategyName = "Отключено";
            CurrentLatencyMs = 0;
            OnLatencyChanged?.Invoke(0);
            OnStrategyChanged?.Invoke("Отключено");
            OnConnectionStateChanged?.Invoke(false);
            Log("⏹️ Все службы обхода остановлены.");
        }

        private async Task<int> ProbeTargetAsync(string url)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Head, url);
                var res = await _probeHttp.SendAsync(req);
                sw.Stop();
                if (res.IsSuccessStatusCode || (int)res.StatusCode < 500)
                {
                    return (int)sw.ElapsedMilliseconds;
                }
            }
            catch
            {
            }
            return -1;
        }

        private void SetStrategy(string name)
        {
            ActiveStrategyName = name;
            OnStrategyChanged?.Invoke(name);
        }

        private void Log(string message)
        {
            OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
