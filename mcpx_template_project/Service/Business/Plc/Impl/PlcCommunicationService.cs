using System.Collections.Concurrent;
using McpXLib;
using McpXLib.Enums;
using Serilog;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Converters.Plc;
using mcpx_template_project.Helper;

namespace mcpx_template_project.Service.Business.Plc
{
    /// <summary>
    /// 三菱Q系列PLC通讯引擎实现
    /// 使用 McpX 库（MC协议 / 3E帧 / TCP / 二进制 / ProcessorSeries.Q）
    /// 管理多路PLC连接，按区域配置批量读取，暂存最新值供监控模块和暂存窗口使用。
    /// </summary>
    /// <author>Rudeus</author>
    public class PlcCommunicationService : IPlcCommunicationService
    {
        /// <summary>各通讯设定对应的 McpX 实例</summary>
        private readonly ConcurrentDictionary<long, McpX> _connections = new();

        /// <summary>各通讯设定对应的暂存点位列表</summary>
        private readonly ConcurrentDictionary<long, List<PointValueVO>> _cache = new();

        /// <summary>位设备集合：这些 Prefix 以位(bit)为单位读取</summary>
        private static readonly HashSet<Prefix> BitPrefixes = new()
        {
            Prefix.X, Prefix.Y, Prefix.M, Prefix.L, Prefix.F, Prefix.V,
            Prefix.B, Prefix.S, Prefix.SM, Prefix.SB, Prefix.DX, Prefix.DY,
            Prefix.TS, Prefix.TC, Prefix.SS, Prefix.SC, Prefix.CS, Prefix.CC
        };

        /// <summary>最近一次连接失败的错误描述（含PLC错误码翻译和排查建议）</summary>
        public string? LastError { get; private set; }

        /// <summary>
        /// 三菱MC协议常见结束代码十六进制翻译表 // TODO 待确认。
        /// </summary>
        private static readonly Dictionary<string, string> PlcErrorMap = new()
        {
            ["C200"] = "C200 : Remote password error 远程密码错误",
            ["C201"] = "不支持的子命令 — 设备代号或读取方式不被PLC支持",
            ["C202"] = "设备不存在 — 请检查设备代号是否正确",
            ["C203"] = "读取点数超出范围 — 请减少Quantity或调整起始地址",
            ["C204"] = "不能读取的设备 — 该设备不允许读取操作",
            ["C205"] = "不能写入的设备 — 该设备不允许写入操作",
            ["C206"] = "读取点数超出限制 — 请减少单次读取数量",
            ["C059"] = "PLC无响应 — 请检查PLC是否在线、IP和端口是否正确",
            ["C05B"] = "PLC型号不匹配 — 请确认PLC型号设置",
            ["C05C"] = "无法在PLC上执行 — PLC可能处于PROG模式或故障状态",
            ["C05D"] = "无法执行（本地模式）— PLC处于本地模式，无法远程通信",
            ["C05E"] = "通信被拒绝 — PLC侧可能已达到最大连接数",
            ["C060"] = "原始通信错误 — 请检查网络连接稳定性",
        };

        /// <summary>
        /// 将PLC异常翻译为包含错误码含义和排查建议的描述。
        /// </summary>
        private static string TranslatePlcError(Exception ex)
        {
            var raw = ex.Message ?? ex.ToString();

            if (raw.Contains("closed", StringComparison.OrdinalIgnoreCase))
            {
                return "TCP连接成功但PLC主动断开了连接。\n\n" +
                       "这说明IP和端口可达，但该端口的应用程序很可能不是「MC Protocol」。\n\n排查建议：\n" +
                       "1. 在GX Works2中打开 PLC参数 → 内置以太网端口设置（或以太网模块参数）\n" +
                       "2. 找到端口1024的配置，确认「应用程序」选择的是「MC Protocol」\n" +
                       "   （而非 MELSOFT 连接——MELSOFT端口同样接受TCP连接但无法响应MC命令）\n" +
                       "3. 若无MC Protocol端口，请新增一个端口号并将应用程序设为「MC Protocol」\n" +
                       "4. 确认通信格式为「二进制」，写入参数后重启PLC\n" +
                       $"5. 原始错误：{raw}";
            }

            foreach (var kv in PlcErrorMap)
            {
                if (raw.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return $"[{kv.Key}] {kv.Value}\n\n排查建议：\n" +
                           "1. 确认PLC侧以太网端口通信协议设置为「MC Protocol」\n" +
                           "2. 确认端口号与通讯设定一致\n" +
                           "3. 确认通信格式（二进制/ASCII）和帧类型（3E/4E帧）匹配\n" +
                           "4. 确认区域配置中设备代号和地址在PLC有效范围内\n" +
                           $"5. 原始错误：{raw}";
                }
            }
            return raw;
        }

        /// <summary>
        /// 帧类型×通信格式组合，依次尝试直到成功。
        /// </summary>
        private static readonly (RequestFrame Frame, bool IsAscii, string Desc)[] FrameConfigs = new[]
        {
            (RequestFrame.E3, false, "3E帧/二进制"),
            (RequestFrame.E4, false, "4E帧/二进制"),
            (RequestFrame.E3, true,  "3E帧/ASCII"),
            (RequestFrame.E4, true,  "4E帧/ASCII"),
        };

        /// <summary>
        /// 建立与PLC的连接
        /// 依次尝试不同帧类型×通信格式组合构造 McpX 实例，
        /// 直到某个组合被PLC接受。连接失败时不抛异常，返回 false 并设置 LastError。
        /// </summary>
        public async Task<bool> ConnectAsync(CommSettingVO setting)
        {
            if (setting == null) return false;

            Disconnect(setting.Id);
            LastError = null;

            var timeout = (ushort)Math.Max(setting.TimeoutMs, 1);
            Exception? lastEx = null;

            foreach (var (frame, isAscii, desc) in FrameConfigs)
            {
                Disconnect(setting.Id);

                McpX? plc = null;

                // 构造 McpX 实例
                try
                {
                    Log.Information("[PLC] 尝试构造 McpX({Desc}) → {Ip}:{Port}", desc, setting.Ip, setting.Port);
                    plc = new McpX(
                        setting.Ip, setting.Port,
                        null,                // 远程密码
                        isAscii,             // 通信格式
                        false,               // TCP
                        frame,               // 帧类型
                        timeout,             // 超时
                        ProcessorSeries.Q    // 三菱Q系列
                    );
                    Log.Information("[PLC] McpX 构造成功({Desc})", desc);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[PLC] McpX 构造失败({Desc}): {Msg}", desc, ex.Message);
                    lastEx = ex;
                    // 构造失败
                    // 尝试下一个帧类型组合
                    // "Connection closed unexpectedly" 说明TCP握手成功但PLC主动断开，
                    bool isPlcProtocolError =
                        ex.Message?.Contains("error code", StringComparison.OrdinalIgnoreCase) == true ||
                        ex.Message?.Contains("closed", StringComparison.OrdinalIgnoreCase) == true ||
                        ex.Message?.Contains("C2", StringComparison.OrdinalIgnoreCase) == true ||
                        ex.Message?.Contains("C0", StringComparison.OrdinalIgnoreCase) == true;
                    if (!isPlcProtocolError)
                    {
                        LastError = $"网络连接失败：{ex.Message}\n\n请检查PLC是否在线、IP地址和端口是否正确。";
                        return false;
                    }
                    continue;
                }

                // 构造成功即连接已建立，首次读取验证
                _connections[setting.Id] = plc;
                _cache[setting.Id] = BuildPointList(setting);

                Log.Information("[PLC] 通讯设定 {Name}(ID:{Id}) 连接成功({Desc}) → {Ip}:{Port}",
                    setting.Name, setting.Id, desc, setting.Ip, setting.Port);

                try
                {
                    await ReadAllPointsAsync(setting.Id);
                }
                catch (Exception ex)
                {
                    // ReadAllPointsAsync 内部已有各点位独立容错，此处异常属于意外
                    Log.Warning(ex, "[PLC] 首次读取异常（不影响连接状态）: {Msg}", ex.Message);
                }

                return true;
            }

            LastError = lastEx != null ? TranslatePlcError(lastEx) : "未知错误";
            return false;
        }

        /// <summary>
        /// 断开连接
        /// Dispose McpX 实例释放TCP资源，保留暂存数据但标记为离线。
        /// </summary>
        public void Disconnect(long settingId)
        {
            if (_connections.TryRemove(settingId, out var plc))
            {
                try { plc.Dispose(); } catch { /* 忽略释放异常 */ }
                Log.Information("[PLC] 通讯设定 ID:{Id} 已断开连接", settingId);
            }

            // 更新暂存点位为离线状态
            if (_cache.TryGetValue(settingId, out var points))
            {
                foreach (var p in points)
                {
                    p.IsOnline = false;
                    p.Value = string.Empty;
                    p.Values.Clear();
                    p.ErrorMessage = "连接已断开";
                    p.UpdateTime = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// 检查指定通讯设定是否已连接。
        /// </summary>
        public bool IsConnected(long settingId) => _connections.ContainsKey(settingId);

        /// <summary>
        /// 写入单个点位值到指定通讯设定对应的 PLC。
        /// 位设备写 bool（ON/OFF），字设备按值类型写对应数值。
        /// 写入失败不抛异常，返回 false。
        /// </summary>
        public async Task<bool> WriteSinglePointAsync(long settingId, string deviceCode, string plcAddress, object value)
        {
            if (!_connections.TryGetValue(settingId, out var plc))
            {
                Log.Warning("[PLC] 写入失败：通讯设定 ID:{Id} 未连接", settingId);
                return false;
            }

            try
            {
                // 解析设备代号
                if (!Enum.TryParse<Prefix>(deviceCode, out var prefix))
                {
                    Log.Warning("[PLC] 写入失败：无法解析设备代号 {Code}", deviceCode);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(plcAddress))
                {
                    Log.Warning("[PLC] 写入失败：PLC地址为空");
                    return false;
                }

                // 位设备：写 bool
                if (BitPrefixes.Contains(prefix))
                {
                    bool bitValue;
                    if (value is bool b)
                        bitValue = b;
                    else
                        bitValue = PlcValueConverter.Convert(value?.ToString(), typeof(bool)) is true;

                    await plc.WriteBoolAsync(prefix, plcAddress, bitValue);
                }
                else
                {
                    // 字设备：按值类型分派到对应写方法
                    await WriteWordAsync(plc, prefix, plcAddress, value);
                }

                Log.Information("[PLC] 写入成功 {Code}{Addr} = {Value}", deviceCode, plcAddress, value);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[PLC] 写入异常 {Code}{Addr}: {Msg}", deviceCode, plcAddress, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 读取指定通讯设定下单个点位的当前值（位设备 "ON"/"OFF"，字设备数值文本）。
        /// </summary>
        public async Task<string?> ReadSinglePointAsync(long settingId, string deviceCode, string plcAddress)
        {
            if (!_connections.TryGetValue(settingId, out var plc))
                return null;

            try
            {
                if (!Enum.TryParse<Prefix>(deviceCode, out var prefix))
                    return null;

                if (string.IsNullOrWhiteSpace(plcAddress))
                    return null;

                if (BitPrefixes.Contains(prefix))
                {
                    var values = await plc.BatchReadAsync<bool>(prefix, plcAddress, 1);
                    return values is { Length: > 0 } ? (values[0] ? "1" : "0") : null;
                }
                else
                {
                    var values = await plc.BatchReadAsync<ushort>(prefix, plcAddress, 1);
                    return values is { Length: > 0 } ? values[0].ToString() : null;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[PLC] 单点读取异常 {Code}{Addr}: {Msg}", deviceCode, plcAddress, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 按指定类型读取单个字设备的当前值（如 float→Float32 占 2 字）。
        /// 读取失败或未连接时返回该类型默认值。
        /// </summary>
        public async Task<T> ReadTypedPointAsync<T>(long settingId, string deviceCode, string plcAddress) where T : unmanaged
        {
            if (!_connections.TryGetValue(settingId, out var plc))
                return default;

            try
            {
                if (!Enum.TryParse<Prefix>(deviceCode, out var prefix))
                    return default;

                if (string.IsNullOrWhiteSpace(plcAddress))
                    return default;

                var values = await plc.BatchReadAsync<T>(prefix, plcAddress, 1);
                return values is { Length: > 0 } ? values[0] : default;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[PLC] 类型化单点读取异常 {Code}{Addr}: {Msg}", deviceCode, plcAddress, ex.Message);
                return default;
            }
        }

        /// <summary>
        /// 更新缓存中某点位的文本值（仅内存缓存，不向 PLC 下发）。
        /// 若该点位不存在则自动补建（upsert），保证机台等固定点位无需依赖区域配置即可被缓存读取。
        /// </summary>
        public void SetCachedValue(long settingId, string deviceCode, string plcAddress, string textValue)
        {
            var points = _cache.GetOrAdd(settingId, _ => new List<PointValueVO>());

            // 先找已有 (设备代号, 地址) 子值更新
            foreach (var point in points)
            {
                if (!string.Equals(point.DeviceCode, deviceCode, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var sub in point.Values)
                {
                    if (string.Equals(sub.PlcAddress, plcAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        sub.Value = textValue;
                        return;
                    }
                }
            }

            // 未命中：补建一个点位（含单个子值）
            points.Add(new PointValueVO
            {
                SettingId = settingId,
                SettingName = string.Empty,
                AreaType = "MACHINE",
                DeviceCode = deviceCode,
                PlcAddress = plcAddress,
                Quantity = 1,
                Value = textValue,
                IsOnline = true,
                ErrorMessage = string.Empty,
                DataType = "字(word)",
                UpdateTime = DateTime.Now,
                Values =
                {
                    new PointSubValueVO { DeviceCode = deviceCode, PlcAddress = plcAddress, Value = textValue }
                }
            });
        }

        /// <summary>
        /// 字设备写入：按运行时值类型分派到 McpX 对应写方法。
        /// 默认按 ushort（与读取一致），int 值超出 ushort 范围时用 int32。
        /// </summary>
        private static async Task WriteWordAsync(McpX plc, Prefix prefix, string address, object value)
        {
            switch (value)
            {
                case bool b:
                    await plc.WriteUInt16Async(prefix, address, (ushort)(b ? 1 : 0));
                    return;
                case byte by:
                    await plc.WriteUInt16Async(prefix, address, by);
                    return;
                case ushort us:
                    await plc.WriteUInt16Async(prefix, address, us);
                    return;
                case short s:
                    await plc.WriteInt32Async(prefix, address, s);
                    return;
                case int i:
                    if (i is >= 0 and <= ushort.MaxValue)
                        await plc.WriteUInt16Async(prefix, address, (ushort)i);
                    else
                        await plc.WriteInt32Async(prefix, address, i);
                    return;
                case uint ui:
                    await plc.WriteUInt32Async(prefix, address, ui);
                    return;
                case long l:
                    await plc.WriteInt64Async(prefix, address, l);
                    return;
                case float f:
                    await plc.WriteSingleAsync(prefix, address, f);
                    return;
                case double d:
                    await plc.WriteDoubleAsync(prefix, address, d);
                    return;
                default:
                    // 兜底：尝试转为数值写 ushort
                    var parsed = PlcValueConverter.Convert(value?.ToString(), typeof(int)) as int? ?? 0;
                    await plc.WriteUInt16Async(prefix, address, (ushort)Math.Clamp(parsed, 0, ushort.MaxValue));
                    return;
            }
        }

        /// <summary>
        /// 批量读取指定通讯设定所有区域配置的PLC点位。
        /// 每个 CommSettingArea 独立读取、独立容错，更新暂存缓存。
        /// </summary>
        public async Task<List<PointValueVO>> ReadAllPointsAsync(long settingId)
        {
            if (!_connections.TryGetValue(settingId, out var plc))
                return new List<PointValueVO>();

            // 获取或初始化暂存点位列表
            var points = _cache.GetOrAdd(settingId, _ => new List<PointValueVO>());

            foreach (var point in points)
            {
                await ReadSinglePointAsync(plc, point);
            }

            // 返回快照副本，避免外部修改影响内部缓存
            return points.ToList();
        }

        /// <summary>
        /// 读取单个点位的值（位设备→bool / 字设备→ushort）。
        /// </summary>
        private static async Task ReadSinglePointAsync(McpX plc, PointValueVO point)
        {
            try
            {
                // 解析设备代号
                if (!Enum.TryParse<Prefix>(point.DeviceCode, out var prefix))
                {
                    point.IsOnline = false;
                    point.ErrorMessage = $"无法解析设备代号: {point.DeviceCode}";
                    point.UpdateTime = DateTime.Now;
                    return;
                }

                if (string.IsNullOrWhiteSpace(point.PlcAddress))
                {
                    point.IsOnline = false;
                    point.ErrorMessage = "PLC起始地址为空";
                    point.UpdateTime = DateTime.Now;
                    return;
                }

                var quantity = (ushort)Math.Max(point.Quantity, 1);

                string[] textValues;
                if (BitPrefixes.Contains(prefix))
                {
                    // 位设备：以 bool 为单位读取，值以 0/1 表示
                    var values = await plc.BatchReadAsync<bool>(prefix, point.PlcAddress, quantity);
                    textValues = values.Select(v => v ? "1" : "0").ToArray();
                    point.DataType = "位(bit)";
                }
                else
                {
                    // 字设备：以 ushort 为单位读取
                    var values = await plc.BatchReadAsync<ushort>(prefix, point.PlcAddress, quantity);
                    textValues = values.Select(v => v.ToString()).ToArray();
                    point.DataType = "字(word)";
                }

                // 展开为子值列表（地址递增），供界面表格展开显示
                UpdateSubValues(point, textValues);
                point.Value = string.Join(", ", textValues);

                point.IsOnline = true;
                point.ErrorMessage = string.Empty;
                point.UpdateTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                point.IsOnline = false;
                point.ErrorMessage = ex.Message;
                point.UpdateTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 根据读取到的文本值展开为子值列表（地址递增）。
        /// 地址进制按设备代号区分（D/M 等十进制、X/Y/B/W 等十六进制），与 McpX 语义一致。
        /// </summary>
        private static void UpdateSubValues(PointValueVO point, string[] textValues)
        {
            // 与暂存列表保持同步：子值个数应等于读取数量
            while (point.Values.Count > textValues.Length)
                point.Values.RemoveAt(point.Values.Count - 1);

            int numberBase = PlcAddressHelper.IsHexDevice(point.DeviceCode) ? 16 : 10;
            bool ok = PlcAddressHelper.TryParseAddress(point.PlcAddress, numberBase, out long startAddr);
            if (!ok) startAddr = 0;

            for (int i = 0; i < textValues.Length; i++)
            {
                var addr = PlcAddressHelper.FormatAddress(startAddr + i, numberBase);
                if (i < point.Values.Count)
                {
                    // 复用已有子值对象，仅更新字段（避免重建对象影响 UI 绑定）
                    point.Values[i].DeviceCode = point.DeviceCode;
                    point.Values[i].PlcAddress = addr;
                    point.Values[i].Value = textValues[i];
                }
                else
                {
                    point.Values.Add(new PointSubValueVO
                    {
                        DeviceCode = point.DeviceCode,
                        PlcAddress = addr,
                        Value = textValues[i]
                    });
                }
            }
        }

        /// <summary>
        /// 获取所有已连接通讯设定的暂存点位数据快照。
        /// </summary>
        public List<PointValueVO> GetCachedPoints()
        {
            var all = new List<PointValueVO>();
            foreach (var kvp in _cache)
            {
                all.AddRange(kvp.Value);
            }
            return all;
        }

        /// <summary>
        /// 获取所有已连接的通讯设定ID。
        /// </summary>
        public List<long> GetConnectedSettingIds() => _connections.Keys.ToList();

        /// <summary>
        /// 根据通讯设定的区域配置，预构建点位值列表。
        /// 每个 CommSettingArea 对应一个 PointValueVO。
        /// </summary>
        private static List<PointValueVO> BuildPointList(CommSettingVO setting)
        {
            var points = new List<PointValueVO>();
            if (setting.Areas == null) return points;

            foreach (var area in setting.Areas)
            {
                points.Add(new PointValueVO
                {
                    SettingId = setting.Id,
                    SettingName = setting.Name,
                    AreaType = area.AreaType,
                    DeviceCode = area.Name,
                    PlcAddress = area.Position,
                    Quantity = area.Quantity,
                    Value = string.Empty,
                    IsOnline = false,
                    ErrorMessage = "等待读取",
                    UpdateTime = DateTime.Now
                });
            }
            return points;
        }
    }
}
