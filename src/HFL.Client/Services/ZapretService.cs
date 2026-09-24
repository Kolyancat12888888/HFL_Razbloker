using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace HFL.Client.Services
{
    public class ZapretService
    {
        private Process? _winwsProcess;
        public bool IsRunning => _winwsProcess != null && !_winwsProcess.HasExited;

        public static readonly string[] DefaultStrategies = new[]
        {
            // Strategy 0: Balanced Enterprise (YouTube + Discord + Web)
            "--wf-tcp=80,443 --wf-udp=443,50000-50050 --dpi-desync=fake,split2 --dpi-desync-autottl=2 --dpi-desync-fooling=md5sig",
            // Strategy 1: Disorder + Split (Aggressive DPI bypass)
            "--wf-tcp=80,443 --wf-udp=443,50000-50050 --dpi-desync=disorder2 --dpi-desync-split-pos=1 --dpi-desync-fooling=badseq",
            // Strategy 2: Fake packet repeats (Ultra resilient)
            "--wf-tcp=80,443 --wf-udp=443,50000-50050 --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-fooling=badsum",
            // Strategy 3: Discord Voice UDP fix
            "--wf-tcp=80,443 --wf-udp=50000-50050 --dpi-desync=fake --dpi-desync-any-protocol --dpi-desync-cutoff=d3"
        };

        public bool Start(string? customArgs = null, int strategyIndex = 0)
        {
            Stop();

            string winwsPath = FindWinwsBinary();
            if (!File.Exists(winwsPath))
            {
                // Create dummy placeholder or log if binary not yet extracted
                return false;
            }

            string args = customArgs ?? DefaultStrategies[Math.Clamp(strategyIndex, 0, DefaultStrategies.Length - 1)];

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = winwsPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(winwsPath) ?? ""
                };

                _winwsProcess = Process.Start(psi);
                return _winwsProcess != null && !_winwsProcess.HasExited;
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
                if (_winwsProcess != null && !_winwsProcess.HasExited)
                {
                    _winwsProcess.Kill(true);
                    _winwsProcess.Dispose();
                }
            }
            catch
            {
            }
            finally
            {
                _winwsProcess = null;
            }

            // Clean up any stray winws instances
            try
            {
                foreach (var p in Process.GetProcessesByName("winws"))
                {
                    try { p.Kill(); } catch { }
                }
            }
            catch { }
        }

        private static string FindWinwsBinary()
        {
            string baseDir = AppContext.BaseDirectory;
            string localBin = Path.Combine(baseDir, "Assets", "bin", "winws.exe");
            if (File.Exists(localBin)) return localBin;

            string rootBin = Path.Combine(baseDir, "winws.exe");
            return rootBin;
        }
    }
}
