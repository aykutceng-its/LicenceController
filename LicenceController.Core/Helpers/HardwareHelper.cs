using System;
using System.Collections.Generic;
using System.Text;
using LicenceController.Core.Interfaces;
using LicenceController.Core.Services;
using System.Linq;
using System.Management;


namespace LicenceController.Core.Helpers
{
    public class HardwareHelper : IHardwareHelper
    {
        public string GetCpuId()
        {
            using var searcher = new ManagementObjectSearcher("select ProcessorId from Win32_Processor");
            var cpuId = searcher.Get().Cast<ManagementObject>().FirstOrDefault()?["ProcessorId"]?.ToString();
            return cpuId.Trim() ?? "UNKNOWN_CPU";
        }

        public string GetDiskSerial()
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMedia");
            var disk = searcher.Get().Cast<ManagementObject>().FirstOrDefault()?["SerialNumber"]?.ToString();
            return disk?.Trim() ?? "UNKNOWN_DISK";
        }

        public string GetMacAddress()
        {
            using var searcher = new ManagementObjectSearcher("SELECT MACAddress FROM Win32_NetworkAdapter WHERE MACAddress IS NOT NULL AND Manufacturer != 'Microsoft'");
            var mac = searcher.Get().Cast<ManagementObject>().FirstOrDefault()?["MACAddress"]?.ToString();
            return mac?.Trim() ?? "UNKNOWN_MAC";
        }

        public string GetHardwareId()
        {
            return ($"{GetCpuId()}|||{GetMacAddress()}|||{GetDiskSerial()}").Trim();
        }
    }

    public static class CachedInfo 
    {
        private static string? _hardwareId;
        public static string GetHardwareId(IHardwareHelper hardwareHelper)
        {
            if (string.IsNullOrEmpty(_hardwareId))
            {
                _hardwareId = hardwareHelper.GetHardwareId() ?? "";
            }
            return _hardwareId;
        }
    }
}
