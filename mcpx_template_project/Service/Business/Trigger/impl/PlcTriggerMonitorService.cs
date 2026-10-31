using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using mcpx_template_project.Core.Logging;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Domain.VO.Trigger;
using mcpx_template_project.Helper;
using mcpx_template_project.Service.Business.Plc;
using PlcTriggerEntity = mcpx_template_project.Domain.Entities.Business.Trigger.PlcTrigger;

namespace mcpx_template_project.Service.Business.Trigger.impl
{
    /// <summary>
    /// PLC 触发器监控服务实现。
    /// 设计要点：
    ///   1. 单线程串行轮询（System.Threading.Timer 触发，_polling 门闩防重入），
    ///      天然避免「同一点重复弹窗」与共享状态并发问题；
    ///   2. 点位值从 <see cref="IPlcCommunicationService.GetCachedPoints"/> 缓存快照读取，
    ///      不直接向 PLC 发请求（由通讯引擎的批量读取周期负责刷新缓存）；
    ///   3. 每个触发器维护「上次值」，on 模式看 OFF→ON 上升沿，change 模式看值变化；
    ///   4. 动作派发：alarm 触发 AlarmRaised（UI 弹窗），event 反射调用事件处理器。
    /// </summary>
    public class PlcTriggerMonitorService : IPlcTriggerMonitorService, IDisposable
    {
        private readonly IPlcTriggerService _triggerService;
        private readonly IPlcCommunicationService _plcService;
        private readonly PlcEventHandlerRegistry _handlerRegistry;
        private readonly ILogService? _logService;

        private readonly object _gate = new();
        private System.Threading.Timer? _timer;
        private bool _running;
        private bool _polling;

        /// <summary>每个触发器的上次值（key = trigger id）。null 表示首次（视为未触发）</summary>
        private readonly ConcurrentDictionary<long, string?> _lastValue = new();

        /// <summary>每个触发器的上次 ON 状态（key = trigger id）</summary>
        private readonly ConcurrentDictionary<long, bool> _lastOn = new();

        /// <summary>轮询周期（毫秒）</summary>
        private const int PollIntervalMs = 1000;

        public ObservableCollection<ActiveTriggerVO> ActiveAlarms { get; } = new();

        public int ActiveAlarmCount
        {
            get { lock (_gate) return ActiveAlarms.Count; }
        }

        public event Action<PlcTriggerEntity>? AlarmRaised;
        public event Action? ActiveAlarmsChanged;

        public PlcTriggerMonitorService(
            IPlcTriggerService triggerService,
            IPlcCommunicationService plcService,
            PlcEventHandlerRegistry handlerRegistry,
            ILogService? logService = null)
        {
            _triggerService = triggerService;
            _plcService = plcService;
            _handlerRegistry = handlerRegistry;
            _logService = logService;
        }

        /// <summary>启动后台轮询（幂等）。</summary>
        public void Start()
        {
            lock (_gate)
            {
                if (_running) return;
                _running = true;

                _timer ??= new System.Threading.Timer(
                    _ => _ = PollOnceAsync(),
                    null,
                    PollIntervalMs,
                    PollIntervalMs);
            }
        }

        /// <summary>停止后台轮询。</summary>
        public void Stop()
        {
            lock (_gate)
            {
                _running = false;
                _timer?.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
        }

        /// <summary>立即执行一次轮询。</summary>
        public async Task PollOnceAsync()
        {
            lock (_gate)
            {
                if (_polling) return; // 防重入：保证单线程串行
                _polling = true;
            }

            try
            {
                // 1. 拉取启用中的触发器配置
                var triggers = await _triggerService.QueryAllAsync();

                // 2. 取一次全量缓存快照（不逐点请求 PLC）
                var cached = _plcService.GetCachedPoints();

                // 3. 本轮活跃的警报集合
                var newActive = new List<ActiveTriggerVO>();

                foreach (var trigger in triggers)
                {
                    if (trigger.Status != "1") continue; // 停用不监控

                    // 设备未连接则跳过
                    if (!_plcService.IsConnected(trigger.CommSettingId))
                        continue;

                    // 从缓存读取当前值（找不到则跳过）
                    var current = ReadFromCache(cached, trigger.DeviceCode, trigger.Address);
                    if (current == null) continue;

                    var previous = _lastValue.TryGetValue(trigger.Id, out var prev) ? prev : null;

                    // 判定是否触发
                    bool shouldFire = EvaluateTrigger(trigger, previous, current);

                    // 更新上次值
                    _lastValue[trigger.Id] = current;

                    // 维护 ON 状态（供警报列表展示）
                    bool isOn = PlcHelper.IsOn(current);
                    _lastOn[trigger.Id] = isOn;

                    if (shouldFire)
                    {
                        await DispatchAsync(trigger, previous, current);
                    }

                    // 警报类型且当前 ON → 计入活跃警报列表
                    if (trigger.IsAlarm && isOn)
                    {
                        newActive.Add(new ActiveTriggerVO
                        {
                            TriggerId = trigger.Id,
                            Name = trigger.Name,
                            TriggerType = trigger.TriggerType,
                            Remark = trigger.Remark,
                            CommSettingId = trigger.CommSettingId,
                            FullAddress = trigger.FullAddress,
                            CurrentValue = current
                        });
                    }
                }

                // 4. 同步活跃警报集合
                if (SyncActiveAlarms(newActive))
                    ActiveAlarmsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "[PLC触发器] 轮询异常");
            }
            finally
            {
                lock (_gate)
                    _polling = false;
            }
        }

        /// <summary>判定触发器是否应触发。</summary>
        private static bool EvaluateTrigger(PlcTriggerEntity trigger, string? previous, string current)
        {
            // 首次（previous 为 null）不触发，避免启动瞬间所有 ON 点集体弹窗
            if (previous == null) return false;

            return trigger.TriggerMode switch
            {
                "change" => !string.Equals(previous, current, StringComparison.OrdinalIgnoreCase),
                _ => PlcHelper.IsOn(current) && !PlcHelper.IsOn(previous), // on 模式：上升沿
            };
        }

        /// <summary>从缓存快照中读取 (设备代号, 地址) 的当前值。</summary>
        private static string? ReadFromCache(List<PointValueVO> cached, string deviceCode, string address)
        {
            foreach (var point in cached)
            {
                if (!string.Equals(point.DeviceCode, deviceCode, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var sub in point.Values)
                {
                    if (string.Equals(sub.PlcAddress, address, StringComparison.OrdinalIgnoreCase))
                        return sub.Value;
                }
            }
            return null;
        }

        /// <summary>派发动作：alarm 抛事件，event 反射调用处理器。</summary>
        private async Task DispatchAsync(PlcTriggerEntity trigger, string? previous, string current)
        {
            if (trigger.IsAlarm)
            {
                AlarmRaised?.Invoke(trigger);
                return;
            }

            // event 类型：反射调用处理器
            if (string.IsNullOrWhiteSpace(trigger.ActionKey))
            {
                _logService?.LogWarning($"[PLC触发器] 事件触发器 {trigger.Name} 未配置 action_key，跳过");
                return;
            }

            var handler = _handlerRegistry.ResolveHandler(trigger.ActionKey);
            if (handler == null)
            {
                _logService?.LogWarning($"[PLC触发器] 未找到事件处理器 {trigger.ActionKey}（触发器：{trigger.Name}），跳过");
                return;
            }

            var context = new PlcEventContext
            {
                TriggerId = trigger.Id,
                CommSettingId = trigger.CommSettingId,
                DeviceCode = trigger.DeviceCode,
                Address = trigger.Address,
                Name = trigger.Name,
                CurrentValue = current,
                PreviousValue = previous,
                Remark = trigger.Remark,
                TriggerTime = DateTime.Now
            };

            try
            {
                await handler.HandleAsync(context);
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, $"[PLC触发器] 事件处理器 {trigger.ActionKey} 执行异常（触发器：{trigger.Name}）");
            }
        }

        /// <summary>同步活跃警报集合，返回是否有变化。</summary>
        private bool SyncActiveAlarms(List<ActiveTriggerVO> newActive)
        {
            lock (_gate)
            {
                var newIds = newActive.Select(a => a.TriggerId).ToHashSet();
                var oldIds = ActiveAlarms.Select(a => a.TriggerId).ToHashSet();

                if (newIds.SetEquals(oldIds)) return false;

                ActiveAlarms.Clear();
                foreach (var a in newActive)
                    ActiveAlarms.Add(a);
                return true;
            }
        }

        public void Dispose()
        {
            Stop();
            _timer?.Dispose();
            _timer = null;
        }
    }
}
