using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HFL.Client.Services
{
    public class DnsClientService
    {
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        private static extern int DnsFlushResolverCache();

        public bool IsDnsApplied { get; private set; }

        public bool SetCustomDns(string primaryDns, string? secondaryDns = "1.1.1.1")
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                         ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                    {
                        string adapterName = ni.Name;
                        ExecuteNetsh($"interface ipv4 set dns name=\"{adapterName}\" static {primaryDns} primary");
                        if (!string.IsNullOrEmpty(secondaryDns))
                        {
                            ExecuteNetsh($"interface ipv4 add dns name=\"{adapterName}\" {secondaryDns} index=2");
                        }
                    }
                }

                FlushDns();
                IsDnsApplied = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void RestoreDhcpDns()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                         ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                    {
                        string adapterName = ni.Name;
                        ExecuteNetsh($"interface ipv4 set dns name=\"{adapterName}\" dhcp");
                    }
                }
                FlushDns();
            }
            catch
            {
            }
            finally
            {
                IsDnsApplied = false;
            }
        }

        public static void FlushDns()
        {
            try
            {
                DnsFlushResolverCache();
            }
            catch { }

            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                p?.WaitForExit(1000);
            }
            catch { }
        }

        private static void ExecuteNetsh(string arguments)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                p?.WaitForExit(2000);
            }
            catch { }
        }
    }
}
