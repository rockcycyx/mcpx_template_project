using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Security.Cryptography;

namespace mcpx_template_project.Helper
{
    public static class HardwareIdHelper
    {
        // 获取 CPU 序列号
        public static string GetCpuId()
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            foreach (var obj in searcher.Get())
                return obj["ProcessorId"]?.ToString() ?? "";
            return "";
        }

        // 获取主板序列号
        public static string GetBaseboardId()
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (var obj in searcher.Get())
                return obj["SerialNumber"]?.ToString() ?? "";
            return "";
        }

        // 获取 BIOS 序列号
        public static string GetBiosId()
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
            foreach (var obj in searcher.Get())
                return obj["SerialNumber"]?.ToString() ?? "";
            return "";
        }

        // 获取 Windows MachineGuid（从注册表读取）
        public static string GetMachineGuid()
        {
            using var key = Microsoft.Win32.Registry.LocalMachine
                .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            return key?.GetValue("MachineGuid")?.ToString() ?? "";
        }

        // 组合机器指纹
        public static string GetFingerprint()
        {
            var raw = $"{GetCpuId()}|{GetBaseboardId()}|{GetBiosId()}|{GetMachineGuid()}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash); // 得到 64 位十六进制字符串
        }
    }
}
