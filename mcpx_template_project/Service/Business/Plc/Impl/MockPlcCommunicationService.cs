using System.Collections.Concurrent;
using mcpx_template_project.Converters.Plc;
using mcpx_template_project.Core.Scanner;
using mcpx_template_project.Domain.Enums;
using mcpx_template_project.Domain.Model.Plc;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Helper;
using RecipeEntity = mcpx_template_project.Domain.Entities.Business.Recipe.Recipe;

namespace mcpx_template_project.Service.Business.Plc
{
    /// <summary>
    /// PLC 通讯引擎 Mock 实现：不建立任何真实网络连接，
    /// 用于在 debug 阶段预览实时监控页面的界面效果与交互。
    /// ConnectAsync 直接返回 true，ReadAllPointsAsync 生成随机的模拟点位值。
    /// 位设备生成 ON/OFF，字设备生成递增/随机的数值。
    /// 额外内置配方点位（写点/读点/触发位/成功位）与配方写入流程的自动模拟，
    /// 便于在没有真实 PLC 时测试配方下发效果。
    /// </summary>
    /// <author>Rudeus</author>
    public class MockPlcCommunicationService : IPlcCommunicationService
    {
        /// <summary>各通讯设定的连接状态标记</summary>
        private readonly ConcurrentDictionary<long, byte> _connected = new();

        /// <summary>各通讯设定对应的暂存点位列表</summary>
        private readonly ConcurrentDictionary<long, List<PointValueVO>> _cache = new();

        /// <summary>各通讯设定配方写点的最新写入值（写地址 → 值）</summary>
        private readonly ConcurrentDictionary<long, ConcurrentDictionary<string, string>> _recipeWriteValues = new();

        /// <summary>随机数生成器（线程安全）</summary>
        private readonly Random _random = new();

        /// <summary>位设备集合：这些 Prefix 以位(bit)为单位读取</summary>
        private static readonly HashSet<string> BitDevices = new(StringComparer.OrdinalIgnoreCase)
        {
            "X", "Y", "M", "L", "F", "V", "B", "S", "SM", "SB", "DX", "DY",
            "TS", "TC", "SS", "SC", "CS", "CC"
        };

        /// <summary>配方专用点位标记（AreaType）</summary>
        private const string RecipeAreaType = "RECIPE";

        /// <summary>配方实体的读写计划（注解扫描，缓存一次）</summary>
        private static readonly PlcObjectPlan RecipePlan =
            PlcPointAttributeScanner.Scan<RecipeEntity>();

        /// <summary>配方业务写值字段</summary>
        private static readonly IReadOnlyList<PlcWriteDefinition> RecipeFields = RecipePlan.WriteValues;

        /// <summary>配方写入触发位定义（M2804）</summary>
        private static readonly PlcWriteDefinition? RecipeTrigger = RecipePlan.Trigger;

        /// <summary>配方回复位定义（M2101，0/1/2）</summary>
        private static readonly PlcWriteDefinition? RecipeReply = RecipePlan.Reply;

        /// <summary>Mock 永不失败，LastError 恒为 null</summary>
        public string? LastError => null;

        /// <summary>
        /// 模拟建立连接：不做任何网络操作，直接返回成功并预构建点位列表。
        /// </summary>
        public Task<bool> ConnectAsync(CommSettingVO setting)
        {
            if (setting == null) return Task.FromResult(false);

            _connected[setting.Id] = 1;
            var points = BuildPointList(setting);
            _cache[setting.Id] = points;

            // 注入配方专用点位（写点/读点/触发位/成功位）
            InjectRecipePoints(setting.Id, points);

            // 立即生成一次模拟值（配方点除外）
            RefreshMockValues(setting.Id);

            return Task.FromResult(true);
        }

        /// <summary>
        /// 模拟断开连接：保留暂存数据但标记为离线。
        /// </summary>
        public void Disconnect(long settingId)
        {
            _connected.TryRemove(settingId, out _);

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

        /// <summary>检查指定通讯设定是否已连接。</summary>
        public bool IsConnected(long settingId) => _connected.ContainsKey(settingId);

        /// <summary>
        /// 模拟写入单个点位：更新缓存中对应子值，返回成功。
        /// 无真实 PLC，写操作直接落到暂存缓存。
        /// 额外处理配方相关写入：写 M2804 触发配方写入模拟，写配方写点记录最新值。
        /// </summary>
        public Task<bool> WriteSinglePointAsync(long settingId, string deviceCode, string plcAddress, object value)
        {
            if (!_cache.TryGetValue(settingId, out var points))
                return Task.FromResult(false);

            var text = PlcValueConverter.ToText(value);

            // 定位到 (设备代号 + 地址) 匹配的子值并更新
            bool updated = false;
            foreach (var point in points)
            {
                if (!string.Equals(point.DeviceCode, deviceCode, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var sub in point.Values)
                {
                    if (string.Equals(sub.PlcAddress, plcAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        sub.Value = text;
                        updated = true;
                    }
                }
            }

            // 配方写点：记录最新写入值
            if (IsRecipeWriteAddress(deviceCode, plcAddress))
            {
                var dict = _recipeWriteValues.GetOrAdd(settingId, _ => new ConcurrentDictionary<string, string>());
                dict[plcAddress] = text;
            }

            // 配方触发位 M2804 置 ON：启动配方写入模拟（PLC 内部处理）
            if (IsRecipeTriggerOn(deviceCode, plcAddress, value))
            {
                _ = SimulateRecipeWriteAsync(settingId);
            }

            return Task.FromResult(updated || IsRecipeAddress(deviceCode, plcAddress));
        }

        /// <summary>
        /// 批量读取指定通讯设定所有区域配置的点位，生成模拟值并更新暂存缓存。
        /// </summary>
        public Task<List<PointValueVO>> ReadAllPointsAsync(long settingId)
        {
            if (!_connected.ContainsKey(settingId))
                return Task.FromResult(new List<PointValueVO>());

            RefreshMockValues(settingId);

            var points = _cache.TryGetValue(settingId, out var list)
                ? list.ToList()
                : new List<PointValueVO>();

            return Task.FromResult(points);
        }

        /// <summary>
        /// 获取所有已连接通讯设定的暂存点位数据快照。
        /// </summary>
        public List<PointValueVO> GetCachedPoints()
        {
            var all = new List<PointValueVO>();
            foreach (var kvp in _cache)
                all.AddRange(kvp.Value);
            return all;
        }

        /// <summary>
        /// 获取所有已连接的通讯设定ID。
        /// </summary>
        public List<long> GetConnectedSettingIds() => _connected.Keys.ToList();

        /// <summary>
        /// 模拟读取单个点位：从缓存读取；缓存不存在时按位/字生成模拟值。
        /// </summary>
        public Task<string?> ReadSinglePointAsync(long settingId, string deviceCode, string plcAddress)
        {
            if (!_connected.ContainsKey(settingId))
                return Task.FromResult<string?>(null);

            // 先从缓存查
            if (_cache.TryGetValue(settingId, out var points))
            {
                foreach (var point in points)
                {
                    if (!string.Equals(point.DeviceCode, deviceCode, StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (var sub in point.Values)
                    {
                        if (string.Equals(sub.PlcAddress, plcAddress, StringComparison.OrdinalIgnoreCase))
                            return Task.FromResult<string?>(sub.Value);
                    }
                }
            }

            // 缓存未命中：按位/字生成模拟值（供警报点位预览）
            bool isBit = BitDevices.Contains(deviceCode ?? string.Empty);
            string value = isBit
                ? (_random.Next(2) == 0 ? "0" : "1")
                : _random.Next(0, 2).ToString(); // 字设备默认生成 0/1，便于模拟警报触发

            return Task.FromResult<string?>(value);
        }

        /// <summary>
        /// 模拟类型化单点读取：从缓存取文本值转换为目标类型，未命中则生成随机模拟值。
        /// </summary>
        public Task<T> ReadTypedPointAsync<T>(long settingId, string deviceCode, string plcAddress) where T : unmanaged
        {
            var text = ReadSinglePointAsync(settingId, deviceCode, plcAddress).Result;
            if (string.IsNullOrEmpty(text))
            {
                // 缓存未命中：按目标类型生成模拟值（float 给随机小数，int 给 0~100）
                if (typeof(T) == typeof(float))
                    return Task.FromResult((T)(object)(float)(_random.NextDouble() * 100f));
                if (typeof(T) == typeof(double))
                    return Task.FromResult((T)(object)(_random.NextDouble() * 100));
                if (typeof(T) == typeof(int))
                    return Task.FromResult((T)(object)_random.Next(0, 100));
                return Task.FromResult(default(T));
            }

            var converted = PlcValueConverter.Convert(text, typeof(T));
            return Task.FromResult(converted is T t ? t : default);
        }

        /// <summary>
        /// 更新缓存中某点位的文本值（仅内存缓存，不向 PLC 下发）。
        /// 若该点位不存在则自动补建（upsert），保证机台等固定点位无需依赖区域配置即可被缓存读取。
        /// </summary>
        public void SetCachedValue(long settingId, string deviceCode, string plcAddress, string textValue)
        {
            var points = _cache.GetOrAdd(settingId, _ => new List<PointValueVO>());

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

        // ============ 配方模拟 ============

        /// <summary>判断是否为配方写地址</summary>
        private static bool IsRecipeWriteAddress(string deviceCode, string address)
        {
            if (!string.Equals(deviceCode, nameof(PlcDevice.D), StringComparison.OrdinalIgnoreCase)) return false;
            return RecipeFields.Any(f => f.Address == address);
        }

        /// <summary>判断是否为配方相关地址（写点/读点/触发位/回复位）</summary>
        private static bool IsRecipeAddress(string deviceCode, string address)
        {
            if (RecipeTrigger != null &&
                string.Equals(deviceCode, RecipeTrigger.DeviceCode.ToString(), StringComparison.OrdinalIgnoreCase))
                return address == RecipeTrigger.Address
                    || (RecipeReply != null && address == RecipeReply.Address);
            if (!string.Equals(deviceCode, nameof(PlcDevice.D), StringComparison.OrdinalIgnoreCase)) return false;
            return RecipeFields.Any(f => f.Address == address)
                || RecipePlan.Reads.Any(r => r.Address == address);
        }

        /// <summary>判断是否写入了配方触发位 M2804 且值为 ON</summary>
        private static bool IsRecipeTriggerOn(string deviceCode, string address, object value)
        {
            if (RecipeTrigger == null) return false;
            if (!string.Equals(deviceCode, RecipeTrigger.DeviceCode.ToString(), StringComparison.OrdinalIgnoreCase)) return false;
            if (address != RecipeTrigger.Address) return false;
            return value is bool b ? b : string.Equals(PlcValueConverter.ToText(value), "ON", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 模拟配方写入流程（三态握手）：
        /// 延迟后把写点值回写到读点，回复位置 ACK(1)；
        /// 等待 PC 将触发位复归 0 后，回复位也复归 0（由 PC 写 0，本模拟配合 PC 侧复归）。
        /// 模拟「PLC 内部处理完成后，将实际配方值回写到读点并应答」的行为。
        /// </summary>
        private async Task SimulateRecipeWriteAsync(long settingId)
        {
            // 模拟 PLC 处理耗时
            await Task.Delay(800);

            if (!_cache.TryGetValue(settingId, out var points)) return;

            var writeValues = _recipeWriteValues.GetOrAdd(settingId, _ => new ConcurrentDictionary<string, string>());

            // 把写点最新值回写到对应读点
            foreach (var field in RecipeFields)
            {
                if (!writeValues.TryGetValue(field.Address, out var val)) continue;
                var readDef = RecipePlan.Reads.FirstOrDefault(r => r.PropertyName == field.PropertyName);
                if (readDef == null) continue;
                SetSubValue(points, readDef.DeviceCode, readDef.Address, val);
            }

            // 回复位置 ACK(1)
            if (RecipeReply != null)
                SetSubValue(points, RecipeReply.DeviceCode, RecipeReply.Address, "1");

            // 等待 PC 将触发位复归 0（回复位复归由 PC 主动写 0 完成，本模拟仅在此等待触发位回 0）
            if (RecipeReply != null && RecipeTrigger != null)
            {
                _ = Task.Run(async () =>
                {
                    // 轮询触发位，直到 PC 置回 0
                    var deadline = DateTime.Now.AddSeconds(10);
                    while (DateTime.Now < deadline)
                    {
                        var trigger = RecipeTrigger.DeviceCode.ToString();
                        if (TriggerIsOff(points, trigger, RecipeTrigger.Address))
                            break;
                        await Task.Delay(100);
                    }
                });
            }
        }

        /// <summary>判断缓存中触发位是否为「OFF/0/false」。</summary>
        private static bool TriggerIsOff(List<PointValueVO> points, string deviceCode, string address)
        {
            foreach (var point in points)
            {
                if (!string.Equals(point.DeviceCode, deviceCode, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var sub in point.Values)
                {
                    if (string.Equals(sub.PlcAddress, address, StringComparison.OrdinalIgnoreCase))
                        return !PlcHelper.IsOn(sub.Value);
                }
            }
            return true; // 未找到视为已复位，避免死循环
        }

        /// <summary>更新指定 (设备代号, 地址) 子值</summary>
        private static void SetSubValue(List<PointValueVO> points, PlcDevice deviceCode, string address, string value)
        {
            var code = deviceCode.ToString();
            foreach (var point in points)
            {
                if (!string.Equals(point.DeviceCode, code, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var sub in point.Values)
                {
                    if (string.Equals(sub.PlcAddress, address, StringComparison.OrdinalIgnoreCase))
                    {
                        sub.Value = value;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 注入配方专用点位：写点、读点、触发位、成功位。
        /// 每个点位 Quantity=1，初始值：字设备 "0"，位设备 "OFF"。
        /// </summary>
        private static void InjectRecipePoints(long settingId, List<PointValueVO> points)
        {
            // 写点与读点（按地址去重）
            var addressSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in RecipeFields)
            {
                AddRecipePoint(points, settingId, field.DeviceCode, field.Address, "0", addressSet);
            }

            foreach (var read in RecipePlan.Reads)
            {
                AddRecipePoint(points, settingId, read.DeviceCode, read.Address, "0", addressSet);
            }

            // 触发位与回复位
            if (RecipeTrigger != null)
                AddRecipePoint(points, settingId, RecipeTrigger.DeviceCode, RecipeTrigger.Address, "0", addressSet);
            if (RecipeReply != null)
                AddRecipePoint(points, settingId, RecipeReply.DeviceCode, RecipeReply.Address, "0", addressSet);
        }

        private static void AddRecipePoint(
            List<PointValueVO> points,
            long settingId,
            PlcDevice deviceCode,
            string address,
            string initialValue,
            HashSet<string> addressSet)
        {
            var code = deviceCode.ToString();
            if (!addressSet.Add(code + ":" + address)) return;

            bool isBit = BitDevices.Contains(code);

            points.Add(new PointValueVO
            {
                SettingId = settingId,
                SettingName = "配方",
                AreaType = RecipeAreaType,
                DeviceCode = code,
                PlcAddress = address,
                Quantity = 1,
                Value = initialValue,
                IsOnline = true,
                ErrorMessage = string.Empty,
                DataType = isBit ? "位(bit)" : "字(word)",
                UpdateTime = DateTime.Now,
                Values = { new PointSubValueVO { DeviceCode = code, PlcAddress = address, Value = initialValue } }
            });
        }

        // ============ 原有逻辑 ============

        /// <summary>
        /// 根据通讯设定的区域配置，预构建点位值列表。
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

        /// <summary>
        /// 为指定通讯设定的所有点位生成模拟值。
        /// 位设备生成 ON/OFF，字设备生成随机数值（随刷新周期变化）。
        /// 配方专用点位（AreaType=RECIPE）不参与随机生成，避免覆盖写入值。
        /// </summary>
        private void RefreshMockValues(long settingId)
        {
            if (!_cache.TryGetValue(settingId, out var points)) return;

            foreach (var point in points)
            {
                if (point.AreaType == RecipeAreaType) continue; // 配方点不随机化

                var quantity = Math.Max(point.Quantity, 1);
                bool isBit = BitDevices.Contains(point.DeviceCode ?? string.Empty);

                var textValues = new string[quantity];
                for (int i = 0; i < quantity; i++)
                {
                    textValues[i] = isBit
                        ? (_random.Next(2) == 0 ? "0" : "1")
                        : _random.Next(0, 65535).ToString();
                }

                point.DataType = isBit ? "位(bit)" : "字(word)";
                UpdateSubValues(point, textValues);
                point.Value = string.Join(", ", textValues);
                point.IsOnline = true;
                point.ErrorMessage = string.Empty;
                point.UpdateTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 根据读取到的文本值展开为子值列表（地址递增）。
        /// 地址进制按设备代号区分（D/M 等十进制、X/Y/B/W 等十六进制），与 McpX 语义一致。
        /// </summary>
        private static void UpdateSubValues(PointValueVO point, string[] textValues)
        {
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
    }
}
