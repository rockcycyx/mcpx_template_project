using System.Globalization;
using Secs4Net;
using Serilog;
using mcpx_template_project.Service.Business.Plc;
using SecsVidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsVid;
using SecsDataType = mcpx_template_project.Domain.Entities.Business.Secs.SecsDataType;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// SECS VID 值读取门面实现。
    /// source=db   → 取 <see cref="SecsVidEntity.DefaultValue"/> 文本，按 data_type 转 Item；
    /// source=plc  → 经 <see cref="IPlcCommunicationService.ReadSinglePointAsync"/> 读点位文本，再按 data_type 转 Item。
    /// 转换失败 / 未连接 / VID 停用 → Exists=false。
    /// </summary>
    public sealed class SecsValueReader : ISecsValueReader
    {
        private readonly IPlcCommunicationService _plcService;

        public SecsValueReader(IPlcCommunicationService plcService)
        {
            _plcService = plcService;
        }

        public async Task<SecsValueResult> ReadAsync(long instanceId, long? commSettingId, SecsVidEntity vid)
        {
            // 停用的 VID 不参与查询
            if (vid == null || vid.Status == "1")
                return new SecsValueResult { VidId = vid?.VidId ?? 0, Exists = false };

            string? text;
            if (vid.IsPlcSource)
            {
                // PLC 来源：无关联通讯设定或未连接时视为不存在
                if (!commSettingId.HasValue)
                {
                    Log.Warning("[SECS] VID {VidId}({Name}) 读取失败：secs_instance.comm_setting_id 为空，无法定位 PLC", vid.VidId, vid.Name);
                    return new SecsValueResult { VidId = vid.VidId, Exists = false };
                }
                if (!_plcService.IsConnected(commSettingId.Value))
                {
                    Log.Warning("[SECS] VID {VidId}({Name}) 读取失败：PLC 通讯设定 {SettingId} 未连接", vid.VidId, vid.Name, commSettingId.Value);
                    return new SecsValueResult { VidId = vid.VidId, Exists = false };
                }

                // 兼容把设备代号写进地址的情况（如 device_code 空、plc_address="D770"）
                var (deviceCode, address) = SplitDeviceAddress(vid.DeviceCode, vid.PlcAddress);
                if (address.Length == 0)
                {
                    Log.Warning("[SECS] VID {VidId}({Name}) 读取失败：plc_address 为空", vid.VidId, vid.Name);
                    return new SecsValueResult { VidId = vid.VidId, Exists = false };
                }

                text = await _plcService.ReadSinglePointAsync(commSettingId.Value, deviceCode, address);
                if (text == null)
                {
                    Log.Warning("[SECS] VID {VidId}({Name}) 读取失败：PLC 点位 {Device}{Addr} 返回 null（设备代号无法解析/地址非法/通讯异常）",
                        vid.VidId, vid.Name, deviceCode, address);
                    return new SecsValueResult { VidId = vid.VidId, Exists = false };
                }
                Log.Debug("[SECS] VID {VidId}({Name}) PLC {Device}{Addr} = {Value}", vid.VidId, vid.Name, deviceCode, address, text);
            }
            else
            {
                text = vid.DefaultValue;
                if (text == null)
                    return new SecsValueResult { VidId = vid.VidId, Exists = false };
            }

            var item = Convert(vid.DataType, text);
            if (item == null)
            {
                Log.Warning("[SECS] VID {VidId} 值转换失败：type={Type}, value={Value}", vid.VidId, vid.DataType, text);
                return new SecsValueResult { VidId = vid.VidId, Exists = false };
            }

            return new SecsValueResult { VidId = vid.VidId, Exists = true, Value = item };
        }

        public Item? Convert(SecsDataType dataType, string? textValue)
        {
            if (textValue == null) return null;
            var s = textValue.Trim();

            try
            {
                return dataType switch
                {
                    SecsDataType.A => Item.A(s),
                    SecsDataType.B => Item.B(ParseBytes(s)),
                    SecsDataType.BOOLEAN => Item.Boolean(ParseBool(s)),
                    SecsDataType.I1 => Item.I1(sbyte.Parse(s, CultureInfo.InvariantCulture)),
                    SecsDataType.I2 => Item.I2(short.Parse(s, CultureInfo.InvariantCulture)),
                    SecsDataType.I4 => Item.I4(int.Parse(s, CultureInfo.InvariantCulture)),
                    SecsDataType.I8 => Item.I8(long.Parse(s, CultureInfo.InvariantCulture)),
                    SecsDataType.U1 => Item.U1(byte.Parse(s, CultureInfo.InvariantCulture)),
                    SecsDataType.U2 => Item.U2(ushort.Parse(s, CultureInfo.InvariantCulture)),
                    SecsDataType.U4 => Item.U4(uint.Parse(s, CultureInfo.InvariantCulture)),
                    SecsDataType.U8 => Item.U8(ulong.Parse(s, CultureInfo.InvariantCulture)),
                    SecsDataType.F4 => Item.F4(float.Parse(s, CultureInfo.InvariantCulture)),
                    SecsDataType.F8 => Item.F8(double.Parse(s, CultureInfo.InvariantCulture)),
                    _ => null, // List 类型不支持标量值转换
                };
            }
            catch
            {
                return null;
            }
        }

        private static bool ParseBool(string s)
            => s is "1" or "true" or "TRUE" or "True" or "ON" or "on";

        /// <summary>
        /// 拆分设备代号与地址。device_code 已填则直接使用；
        /// 否则从 plc_address 提取前导字母作为设备代号（如 "D770" → ("D", "770")），纯数字则代号留空。
        /// </summary>
        private static (string DeviceCode, string Address) SplitDeviceAddress(string? deviceCode, string? plcAddress)
        {
            var addr = plcAddress?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(deviceCode))
                return (deviceCode.Trim(), addr);

            int i = 0;
            while (i < addr.Length && char.IsLetter(addr[i])) i++;
            return i > 0 ? (addr[..i], addr[i..]) : (string.Empty, addr);
        }

        private static byte[] ParseBytes(string s)
        {
            // 二进制：支持 "A1 B2" 十六进制串或原始 ASCII 字节
            var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return Array.Empty<byte>();

            // 尝试按十六进制解析
            try
            {
                return tokens.Select(t => System.Convert.ToByte(t, 16)).ToArray();
            }
            catch
            {
                // 回退：按 ASCII 字节
                return System.Text.Encoding.ASCII.GetBytes(s);
            }
        }
    }
}
