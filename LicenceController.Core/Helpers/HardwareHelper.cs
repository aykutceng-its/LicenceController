using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using LicenceController.Core.Interfaces;

namespace LicenceController.Core.Helpers
{
    public class HardwareHelper : IHardwareHelper
    {
        public string GetHardwareId()
        {
            // 1. Önce Docker/Script ile sızdırılan 'GERÇEK' ID var mı bak (En Güvenli Yol)
            var hostCpu = Environment.GetEnvironmentVariable("REAL_CPUID");
            var hostDisk = Environment.GetEnvironmentVariable("REAL_DISKID");
            
            if (!string.IsNullOrEmpty(hostCpu) && !string.IsNullOrEmpty(hostDisk))
                return $"{hostCpu.Trim()}|||{GetMacAddress()}|||{hostDisk.Trim()}";

            // 2. İşletim sistemine göre yerel sorgu yap
            string cpu = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? GetWindowsCpu() : GetLinuxCpu();
            string disk = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? GetWindowsDisk() : GetLinuxDisk();
            
            return $"{cpu}|||{GetMacAddress()}|||{disk}";
        }

        private string GetWindowsCpu()
        {
            try {
                // Windows bağımlılığını korumak için string bazlı çağrım (Reflection alternatifi)
                return RunCommand("wmic", "cpu get processorid").Split('\n').ElementAtOrDefault(1)?.Trim() ?? "WIN_CPU_UNK";
            } catch { return "WIN_CPU_ERR"; }
        }

        private string GetWindowsDisk()
        {
            try {
                return RunCommand("wmic", "diskdrive get serialnumber").Split('\n').ElementAtOrDefault(1)?.Trim() ?? "WIN_DISK_UNK";
            } catch { return "WIN_DISK_ERR"; }
        }

        private string GetLinuxCpu() => RunCommand("sh", "-c \"lscpu | grep 'Model name' | cut -d: -f2\"").Trim();
        
        private string GetLinuxDisk() => RunCommand("sh", "-c \"lsblk -d -no serial | head -n 1\"").Trim();

        public string GetMacAddress()
        {
            // 1. Önce dışarıdan sızdırılan gerçek MAC var mı bak
            var hostMac = Environment.GetEnvironmentVariable("HOST_MAC");
            if (!string.IsNullOrEmpty(hostMac)) return CleanMac(hostMac);

            // 2. Yoksa konteyner içindeki kartlara bak (network_mode: host ise burası gerçek kartı verir)
            var mac = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();

            return CleanMac(mac);
        }

        private string CleanMac(string mac) {
            if (string.IsNullOrEmpty(mac)) return "00:00:00:00:00:00";
            string clean = mac.Replace(":", "").Replace("-", "").ToUpper();
            return string.Join(":", Enumerable.Range(0, 6).Select(i => clean.Substring(i * 2, 2)));
        }

        private string RunCommand(string fileName, string args)
        {
            try {
                var process = new Process {
                    StartInfo = new ProcessStartInfo {
                        FileName = fileName, Arguments = args, RedirectStandardOutput = true,
                        UseShellExecute = false, CreateNoWindow = true
                    }
                };
                process.Start();
                string result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return result;
            } catch { return ""; }
        }

        // Interface gereği boş metodlar
        public string GetCpuId() => GetHardwareId().Split("|||")[0];
        public string GetDiskSerial() => GetHardwareId().Split("|||")[2];
    }
}