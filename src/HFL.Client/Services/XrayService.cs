using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace HFL.Client.Services
{
    public class XrayService
    {
        private Process? _xrayProcess;
        public bool IsRunning => _xrayProcess != null && !_xrayProcess.HasExited;

        public bool Start(string vlessUri, bool directRuRouting = true)
        {
            Stop();

            string corePath = FindXrayBinary();
            if (!File.Exists(corePath))
            {
                return false;
            }

            string configPath = GenerateConfig(vlessUri, directRuRouting);

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = corePath,
                    Arguments = $"run -c \"{configPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(corePath) ?? ""
                };

                _xrayProcess = Process.Start(psi);
                return _xrayProcess != null && !_xrayProcess.HasExited;
            }
            catch
            {
                return false;
            }
        }

        public void Stop()
        {
            try
            {
                if (_xrayProcess != null && !_xrayProcess.HasExited)
                {
                    _xrayProcess.Kill(true);
                    _xrayProcess.Dispose();
                }
            }
            catch { }
            finally
            {
                _xrayProcess = null;
            }

            try
            {
                foreach (var p in Process.GetProcessesByName("xray"))
                {
                    try { p.Kill(); } catch { }
                }
            }
            catch { }
        }

        private string GenerateConfig(string vlessUri, bool directRuRouting)
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HFL_Razbloker");
            if (!Directory.Exists(appData)) Directory.CreateDirectory(appData);

            string configPath = Path.Combine(appData, "xray_active.json");

            // High-performance low-latency configuration template with Direct RU split
            var configObj = new
            {
                log = new { loglevel = "warning" },
                inbounds = new object[]
                {
                    new
                    {
                        port = 10808,
                        listen = "127.0.0.1",
                        protocol = "socks",
                        settings = new { auth = "noauth", udp = true }
                    },
                    new
                    {
                        port = 10809,
                        listen = "127.0.0.1",
                        protocol = "http"
                    }
                },
                outbounds = new object[]
                {
                    new
                    {
                        tag = "proxy",
                        protocol = "freedom" // Default fallback or populated from vlessUri parser
                    },
                    new
                    {
                        tag = "direct",
                        protocol = "freedom"
                    },
                    new
                    {
                        tag = "block",
                        protocol = "blackhole"
                    }
                },
                routing = new
                {
                    domainStrategy = "IPIfNonMatch",
                    rules = new object[]
                    {
                        new
                        {
                            type = "field",
                            outboundTag = "direct",
                            ip = new[] { "geoip:private", "geoip:ru" }
                        },
                        new
                        {
                            type = "field",
                            outboundTag = "direct",
                            domain = new[] { "geosite:ru", "domain:ru", "domain:su", "domain:xn--p1ai" }
                        },
                        new
                        {
                            type = "field",
                            outboundTag = "proxy",
                            network = "tcp,udp"
                        }
                    }
                }
            };

            string json = JsonSerializer.Serialize(configObj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
            return configPath;
        }

        private static string FindXrayBinary()
        {
            string baseDir = AppContext.BaseDirectory;
            string localBin = Path.Combine(baseDir, "Assets", "bin", "xray.exe");
            if (File.Exists(localBin)) return localBin;

            return Path.Combine(baseDir, "xray.exe");
        }
    }
}
