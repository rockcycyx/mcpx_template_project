using System;
using System.Globalization;

namespace mcpx_template_project.Helper
{
    /// <summary>
    /// PLC 地址计算工具：把「段起始地址 + 段内偏移」解析为实际地址。
    /// 地址进制按设备代号区分（与三菱 McpX 的 DeviceConverter.IsHexDevice 语义一致）：
    ///   X/Y/B/W/SB/SW/DX/DY 为十六进制设备，其余（D/M/L/F 等）为十进制设备。
    /// 十进制设备地址按十进制解析与递增；十六进制设备按十六进制解析，结果大写、无前缀。
    /// </summary>
    public static class PlcAddressHelper
    {
        /// <summary>十六进制设备代号集合（与 McpX DeviceConverter.IsHexDevice 白名单一致）。</summary>
        private static readonly System.Collections.Generic.HashSet<string> HexDevices = new(StringComparer.OrdinalIgnoreCase)
        {
            "X", "Y", "B", "W", "SB", "SW", "DX", "DY"
        };

        /// <summary>判断指定设备代号是否按十六进制解析地址。</summary>
        public static bool IsHexDevice(string? deviceCode) => HexDevices.Contains(deviceCode ?? string.Empty);

        /// <summary>
        /// 计算段起始地址偏移 offset 个位置后的实际地址。
        /// </summary>
        /// <param name="segmentStart">段起始地址，如 "500"（D区十进制）或 "1F"（X区十六进制）</param>
        /// <param name="offset">段内偏移（0 起）</param>
        /// <param name="deviceCode">设备代号，用于决定进制；为空时按十进制</param>
        /// <returns>实际地址字符串；解析失败时返回原段起始地址。</returns>
        public static string Resolve(string? segmentStart, int offset, string? deviceCode = null)
        {
            if (string.IsNullOrWhiteSpace(segmentStart)) return segmentStart ?? string.Empty;
            if (offset <= 0) return segmentStart;

            bool isHex = IsHexDevice(deviceCode);
            int numberBase = isHex ? 16 : 10;

            if (!TryParseAddress(segmentStart, numberBase, out long startAddr))
                return segmentStart;

            return isHex
                ? (startAddr + offset).ToString("X", CultureInfo.InvariantCulture)
                : (startAddr + offset).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 按指定进制解析地址。解析失败返回 false。
        /// </summary>
        public static bool TryParseAddress(string? address, int numberBase, out long value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(address)) return false;

            return numberBase == 16
                ? long.TryParse(address, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)
                : long.TryParse(address, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// 按指定进制格式化地址（十六进制时大写、无前缀，十进制直接数值）。
        /// </summary>
        public static string FormatAddress(long value, int numberBase)
        {
            return numberBase == 16
                ? value.ToString("X", CultureInfo.InvariantCulture)
                : value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
