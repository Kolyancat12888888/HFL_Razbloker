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

        public StrategyOptimizer(ZapretService zapret, DnsClientService dns, LicenseClientService license)
        {
            _zapret = zapret;
            _dns = dns;
            _license = license;
            _probeHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        }

        public async Task StartAutonomousAsync(AppSettings settings, int manualStrategyIndex = -1)
        {
            Stop();
            _isRunning = true;
            _cts = new CancellationTokenSource();
            OnConnectionStateChanged?.Invoke(true);

            Log("🚀 Запуск Zapret DPI Bypass («Швейцарские часы»)...");

            var token = _cts.Token;

            if (manualStrategyIndex >= 0)
            {
                string name = manualStrategyIndex switch
                {
                    0 => "Zapret: YouTube + Discord 4K (Fake TLS/QUIC)",
                    1 => "Zapret: Disorder + BadSeq",
                    2 => "Zapret: Fake Repeats + AutoTTL",
                    3 => "Zapret: Discord Voice Fix",
                    _ => $"Zapret Профиль {manualStrategyIndex + 1}"
                };

                SetStrategy(name);
                _zapret.Start(null, manualStrategyIndex);
                Log($"⚡ Активирован выбранный профиль: {name}");
            }
            else
            {
                // Full Autonomous Auto Mode
                await RunAutonomousFallbackLoopAsync(token);
            }

            _ = MonitorHealthLoopAsync(token);
        }

        private async Task RunAutonomousFallbackLoopAsync(CancellationToken token)
        {
            Log("⚙️ Проверка Стратегии 1: YouTube 4K + Discord Voice (Fake TLS / QUIC)...");
            SetStrategy("Zapret: Стратегия 1 (YouTube + Discord)");
            _zapret.Start(null, 0);

            await Task.Delay(1500, token);
            int ping = await ProbeTargetAsync("https://www.youtube.com");

            if (ping > 0 && ping <= 120)
            {
                CurrentLatencyMs = ping;
                OnLatencyChanged?.Invoke(ping);
                Log($"✅ Стратегия 1 успешно работает! Задержка: {ping} мс (Швейцарская точность)");
                return;
            }

            Log("⚠️ Провайдер фильтрует профиль 1. Переключение на Стратегию 2 (Disorder2)...");
            SetStrategy("Zapret: Стратегия 2 (Disorder)");
            _zapret.Start(null, 1);

            await Task.Delay(1500, token);
            ping = await ProbeTargetAsync("https://www.youtube.com");

            if (ping > 0)
            {
                CurrentLatencyMs = ping;
                OnLatencyChanged?.Invoke(ping);
                Log($"✅ Стратегия 2 зафиксирована! Задержка: {ping} мс");
                return;
            }

            Log("⚡ Активация универсальной Стратегии 3 (Fake Repeats + AutoTTL)...");
            SetStrategy("Zapret: Стратегия 3 (Fake Repeats)");
            _zapret.Start(null, 2);
        }

        private async Task MonitorHealthLoopAsync(CancellationToken token)
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
            _dns.DisableTransparentDns();

            _isRunning = false;
            ActiveStrategyName = "Отключено";
            CurrentLatencyMs = 0;
            OnLatencyChanged?.Invoke(0);
            OnStrategyChanged?.Invoke("Отключено");
            OnConnectionStateChanged?.Invoke(false);
            Log("⏹️ Zapret остановлен. Прямой трафик восстановлен.");
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
