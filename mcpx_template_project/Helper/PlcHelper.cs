using mcpx_template_project.Domain.Enums;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Helper
{
    /// <summary>
    /// PLC 点位处理的纯工具方法集合（无状态、无依赖），
    /// 供读写门面、Mock 通讯层等复用，避免魔法字符串与重复逻辑。
    /// </summary>
    public static class PlcHelper
    {
        /// <summary>位设备代号集合（以位为单位读取/写入）</summary>
        private static readonly HashSet<string> BitDevices = new(StringComparer.OrdinalIgnoreCase)
        {
            "X", "Y", "M", "L", "F", "V", "B", "S", "SM", "SB", "DX", "DY",
            "TS", "TC", "SS", "SC", "CS", "CC"
        };

        /// <summary>判断设备代号是否为位设备（以位为单位）。</summary>
        public static bool IsBitDevice(string? deviceCode)
        {
            return !string.IsNullOrEmpty(deviceCode) && BitDevices.Contains(deviceCode);
        }

        /// <summary>回复位值：无回应（指令处理中）</summary>
        public const int ReplyPending = 0;

        /// <summary>回复位值：ACK（成功）</summary>
        public const int ReplyAck = 1;

        /// <summary>回复位值：NAK（失败）</summary>
        public const int ReplyNak = 2;

        /// <summary>
        /// 判断文本是否表示「ON / 1 / true」。
        /// </summary>
        public static bool IsOn(string? text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            var v = text.Trim();
            return v.Equals("ON", StringComparison.OrdinalIgnoreCase)
                || v == "1"
                || v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 解析回复位文本为三态值：0=无回应 / 1=ACK / 2=NAK。
        /// 无法识别时返回 <see cref="ReplyPending"/>。
        /// </summary>
        public static int ParseReply(string? text)
        {
            if (string.IsNullOrEmpty(text)) return ReplyPending;
            var v = text.Trim();

            if (v == "0") return ReplyPending;
            if (v == "1") return ReplyAck;
            if (v == "2") return ReplyNak;

            if (v.Equals("ACK", StringComparison.OrdinalIgnoreCase)) return ReplyAck;
            if (v.Equals("NAK", StringComparison.OrdinalIgnoreCase)) return ReplyNak;

            return ReplyPending;
        }

        /// <summary>
        /// 从缓存点位列表中读取某个 (设备代号, 地址) 的当前值文本。
        /// 找不到返回 null。
        /// </summary>
        public static string? ReadCachedValue(List<PointValueVO> cached, PlcDevice deviceCode, string address)
        {
            var code = deviceCode.ToString();
            foreach (var point in cached)
            {
                if (!string.Equals(point.DeviceCode, code, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var sub in point.Values)
                {
                    if (string.Equals(sub.PlcAddress, address, StringComparison.OrdinalIgnoreCase))
                        return sub.Value;
                }
            }
            return null;
        }
    }
}
