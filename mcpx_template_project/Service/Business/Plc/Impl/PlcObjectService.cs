using System.Collections.Concurrent;
using mcpx_template_project.Core.Plc;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model.Plc;
using mcpx_template_project.Helper;

namespace mcpx_template_project.Service.Business.Plc.Impl
{
    /// <summary>
    /// 通用 PLC 对象读写门面实现。
    /// 注解扫描、点位分类、触发/回复握手、地址解析、取值下发、读回取值等样板逻辑全部集中在此，
    /// 业务代码只需「赋值 + WriteAsync / Read」。
    /// </summary>
    /// <author>Rudeus</author>
    public class PlcObjectService : IPlcObjectService
    {
        private readonly IPlcCommunicationService _plcService;

        // 每个业务类型 T 缓存一个访问器（含读/写计划 + 表达式树 getter）
        private readonly ConcurrentDictionary<Type, object> _accessors = new();

        public PlcObjectService(IPlcCommunicationService plcService)
        {
            _plcService = plcService;
        }

        /// <inheritdoc />
        public async Task<PlcWriteResult> WriteAsync<T>(long settingId, T obj) where T : class
        {
            if (!_plcService.IsConnected(settingId))
                return new PlcWriteResult { Success = false, Message = "目标通讯设定未连接" };

            var accessor = GetAccessor<T>();
            var plan = accessor.Plan;

            try
            {
                // 1. 逐字段下发写值点位
                foreach (var field in plan.WriteValues)
                {
                    var value = accessor.GetValue(obj, field.PropertyName);
                    await WritePointAsync(settingId, field, value);
                }

                // 2. 有触发位则置 1，通知 PLC 开始处理
                if (plan.Trigger != null)
                    await WritePointAsync(settingId, plan.Trigger, true);

                // 3. 有回复位则轮询等待 ACK/NAK
                if (plan.Reply != null)
                {
                    var reply = await WaitForReplyAsync(settingId, plan.Reply);

                    // 4. 收到回复（ACK/NAK）后触发位复归 0
                    if (plan.Trigger != null)
                        await WritePointAsync(settingId, plan.Trigger, false);

                    // 5. 触发位回 0 后，回复位也复归 0（由 PC 主动写 0）
                    await WritePointAsync(settingId, plan.Reply, 0);

                    return reply == PlcHelper.ReplyAck
                        ? new PlcWriteResult { Success = true, Message = "写入成功（PLC ACK）" }
                        : reply == PlcHelper.ReplyNak
                            ? new PlcWriteResult { Success = false, Message = "写入失败（PLC NAK）" }
                            : new PlcWriteResult { Success = false, Message = "写入超时：PLC 无回应" };
                }

                // 无回复位：仅复位触发位，按写入完成返回
                if (plan.Trigger != null)
                    await WritePointAsync(settingId, plan.Trigger, false);

                return new PlcWriteResult { Success = true, Message = "写入完成（无回复位）" };
            }
            catch (Exception ex)
            {
                // 尽量复位触发位，避免 PLC 停留在接收状态
                if (plan.Trigger != null)
                {
                    try { await WritePointAsync(settingId, plan.Trigger, false); } catch { /* 忽略 */ }
                }
                return new PlcWriteResult { Success = false, Message = "写入异常：" + ex.Message };
            }
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Read<T>(long settingId) where T : class
        {
            var plan = GetAccessor<T>().Plan;
            var cached = _plcService.GetCachedPoints()
                .Where(p => p.SettingId == settingId)
                .ToList();

            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var def in plan.Reads)
            {
                var text = PlcHelper.ReadCachedValue(cached, def.DeviceCode, def.Address);
                result[def.PropertyName] = text ?? string.Empty;
            }
            return result;
        }

        /// <inheritdoc />
        public bool IsConnected(long settingId) => _plcService.IsConnected(settingId);

        /// <inheritdoc />
        public bool IsWriteSuccess<T>(long settingId) where T : class
        {
            var plan = GetAccessor<T>().Plan;
            if (plan.Reply == null) return false;

            var cached = _plcService.GetCachedPoints()
                .Where(p => p.SettingId == settingId)
                .ToList();
            var text = PlcHelper.ReadCachedValue(cached, plan.Reply.DeviceCode, plan.Reply.Address);
            return PlcHelper.ParseReply(text) == PlcHelper.ReplyAck;
        }

        // ---------- 辅助 ----------

        /// <summary>写入单个点位：位设备下发 bool，字设备下发原始值。</summary>
        private Task<bool> WritePointAsync(long settingId, PlcWriteDefinition def, object? value)
        {
            var deviceCode = def.DeviceCode.ToString();
            if (def.DataType == typeof(bool))
                return _plcService.WriteSinglePointAsync(settingId, deviceCode, def.Address, value is true);

            return _plcService.WriteSinglePointAsync(settingId, deviceCode, def.Address, value ?? 0);
        }

        /// <summary>轮询回复位直到变为 ACK/NAK，超时 5s。返回 0/1/2。</summary>
        private async Task<int> WaitForReplyAsync(long settingId, PlcWriteDefinition reply)
        {
            var deadline = DateTime.Now.AddSeconds(5);
            while (DateTime.Now < deadline)
            {
                var cached = _plcService.GetCachedPoints()
                    .Where(p => p.SettingId == settingId)
                    .ToList();
                var text = PlcHelper.ReadCachedValue(cached, reply.DeviceCode, reply.Address);
                var state = PlcHelper.ParseReply(text);
                if (state == PlcHelper.ReplyAck || state == PlcHelper.ReplyNak)
                    return state;
                await Task.Delay(100);
            }
            return PlcHelper.ReplyPending;
        }

        private PlcPointAccessor<T> GetAccessor<T>() where T : class
        {
            return (PlcPointAccessor<T>)_accessors.GetOrAdd(typeof(T), _ => new PlcPointAccessor<T>());
        }
    }
}
