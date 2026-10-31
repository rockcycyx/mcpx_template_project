using Secs4Net;
using Serilog;
using mcpx_template_project.Domain.Entities.Business.Secs;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// Stream 2 Trace 采样上报服务实现。
    /// 每个实例维护 Trace 定义字典（TRID → 采样计划）。采样语义（对齐 SEMI E5 S6F1）：
    ///   - 单个样本采样间隔 = DSPER；
    ///   - 每包 S6F1 上报间隔 = DSPER × REPGSZ，一包内样本数 = REPGSZ；
    ///   - 总采样数 = TOTSMP（SMPLN 从 1 连续递增，达到后自动停用）；
    ///   - 每个样本内数据条数 = SVID 列表长度。
    /// </summary>
    public sealed class SecsTraceService : IS2TraceService, IDisposable
    {
        private readonly ISecsVidService _vidService;
        private readonly ISecsInstanceService _instanceService;
        private readonly ISecsValueReader _valueReader;
        private readonly ISecsGemManager _manager;

        private readonly object _lock = new();
        private readonly Dictionary<long, Dictionary<uint, TracePlan>> _traces = new();

        public SecsTraceService(
            ISecsVidService vidService,
            ISecsInstanceService instanceService,
            ISecsValueReader valueReader,
            ISecsGemManager manager)
        {
            _vidService = vidService;
            _instanceService = instanceService;
            _valueReader = valueReader;
            _manager = manager;
        }

        public Task<byte> InitializeAsync(long instanceId, uint traceId, string dsper, uint totSmp, uint repgsz, IReadOnlyList<int> svids)
        {
            // TOTSMP=0 → 禁用：移除该 Trace
            if (totSmp == 0)
            {
                RemoveTrace(instanceId, traceId);
                return Task.FromResult(SecsAckCodes.TiaackOk);
            }

            // 周期非法 → 拒绝
            if (!TryParseDsper(dsper, out var periodMs))
                return Task.FromResult(SecsAckCodes.TiaackInvalidPeriod);

            // 单 Trace 最多 50 个 SVID
            if (svids.Count > 50)
                return Task.FromResult(SecsAckCodes.TiaackTooManySvids);

            // REPGSZ 非法（0 或大于 TOTSMP）→ 视为周期/参数非法
            if (repgsz == 0 || repgsz > totSmp)
                return Task.FromResult(SecsAckCodes.TiaackInvalidPeriod);

            RegisterTrace(instanceId, traceId, periodMs, totSmp, repgsz, svids);
            return Task.FromResult(SecsAckCodes.TiaackOk);
        }

        public void Clear(long instanceId)
        {
            Dictionary<uint, TracePlan>? plans;
            lock (_lock)
            {
                if (!_traces.Remove(instanceId, out plans)) return;
            }
            foreach (var plan in plans.Values) plan.Stop();
            Log.Information("[SECS] 已清理实例 {InstanceId} 的全部 Trace 定义", instanceId);
        }

        private void RegisterTrace(long instanceId, uint traceId, int periodMs, uint totSmp, uint repgsz, IReadOnlyList<int> svids)
        {
            TracePlan newPlan;
            lock (_lock)
            {
                if (!_traces.TryGetValue(instanceId, out var plans))
                {
                    plans = new Dictionary<uint, TracePlan>();
                    _traces[instanceId] = plans;
                }

                // 已存在的同 TRID → 停止旧计划后覆盖
                if (plans.TryGetValue(traceId, out var oldPlan))
                {
                    oldPlan.Stop();
                }

                newPlan = new TracePlan(instanceId, traceId, periodMs, totSmp, repgsz, svids, this);
                plans[traceId] = newPlan;
            }

            newPlan.Start();
            Log.Information(
                "[SECS] 实例 {InstanceId} 已注册 Trace TRID={Trid}，采样间隔={PeriodMs}ms，总采样={TotSmp}，每包样本数={Repgsz}，SVID={Svids}",
                instanceId, traceId, periodMs, totSmp, repgsz, string.Join(",", svids));
        }

        private void RemoveTrace(long instanceId, uint traceId)
        {
            TracePlan? plan;
            lock (_lock)
            {
                if (!_traces.TryGetValue(instanceId, out var plans)) return;
                if (!plans.Remove(traceId, out plan)) return;
                if (plans.Count == 0) _traces.Remove(instanceId);
            }
            plan.Stop();
            Log.Information("[SECS] 实例 {InstanceId} 已禁用 Trace TRID={Trid}", instanceId, traceId);
        }

        /// <summary>采集一次样本（读取全部 SVID 当前值），返回单个样本 Item。</summary>
        private async Task<Item> CollectSampleAsync(long instanceId, uint sampleNo, IReadOnlyList<int> svids)
        {
            var vids = await _vidService.QueryAllAsync(instanceId, SecsVidType.SVID);
            var byId = vids.ToDictionary(v => v.VidId);
            long? commSettingId = (await _instanceService.GetByIdAsync(instanceId))?.CommSettingId;

            var valueItems = new List<Item>(svids.Count);
            foreach (var svid in svids)
            {
                if (byId.TryGetValue(svid, out var vid))
                {
                    var r = await _valueReader.ReadAsync(instanceId, commSettingId, vid);
                    valueItems.Add(r.Exists && r.Value != null ? r.Value! : Item.L());
                }
                else
                {
                    valueItems.Add(Item.L());
                }
            }

            // 单个样本：L,3 [SMPLN U4, STIME A, L,n [SV...]]
            return Item.L(
                Item.U4(sampleNo),
                Item.A(DateTime.Now.ToString("yyMMddHHmmss")),
                Item.L(valueItems.ToArray()));
        }

        /// <summary>打包一包 S6F1 并主动上报。dataId 用 TRID 标识该 Trace。</summary>
        private async Task SendBatchAsync(long instanceId, uint traceId, IReadOnlyList<Item> samples)
        {
            if (samples.Count == 0) return;

            var deviceKey = instanceId.ToString();
            if (!_manager.IsCommunicating(deviceKey))
            {
                Log.Debug("[SECS] 实例 {InstanceId} 未通信，丢弃 Trace TRID={Trid} 的 {Count} 个样本", instanceId, traceId, samples.Count);
                return;
            }

            // S6F1 = L,2 [DATAID(=TRID) U4, L,n [L,3 [SMPLN, STIME, SV...]...]]
            var msg = new SecsMessage(6, 1, replyExpected: false)
            {
                Name = "TraceDataSend",
                SecsItem = Item.L(Item.U4(traceId), Item.L(samples.ToArray()))
            };

            await _manager.SendAsync(deviceKey, msg, CancellationToken.None);
            Log.Debug("[SECS] Trace TRID={Trid} 上报 S6F1，样本数={Count}", traceId, samples.Count);
        }

        /// <summary>解析 DSPER（hhmmss / hhmmsscc / hhmmsszzz）→ 毫秒周期。</summary>
        internal static bool TryParseDsper(string dsper, out int periodMs)
        {
            periodMs = 0;
            if (string.IsNullOrWhiteSpace(dsper)) return false;
            if (!dsper.All(char.IsDigit)) return false;
            if (dsper.Length is not (6 or 8 or 9)) return false;

            int hh = int.Parse(dsper[..2]);
            int mm = int.Parse(dsper.Substring(2, 2));
            int ss = int.Parse(dsper.Substring(4, 2));
            int frac = dsper.Length switch
            {
                8 => int.Parse(dsper.Substring(6, 2)) * 10,  // cc：厘秒 → 毫秒
                9 => int.Parse(dsper.Substring(6, 3)),        // zzz：毫秒
                _ => 0,
            };

            long total = (hh * 3600L + mm * 60L + ss) * 1000L + frac;
            if (total <= 0) return false;
            periodMs = (int)Math.Min(total, int.MaxValue);
            return true;
        }

        public void Dispose()
        {
            List<TracePlan> all;
            lock (_lock)
            {
                all = _traces.Values.SelectMany(p => p.Values).ToList();
                _traces.Clear();
            }
            foreach (var p in all) p.Stop();
        }

        /// <summary>
        /// 单个 Trace 的采样计划：按 DSPER 定时采集样本并缓冲；
        /// 攒够 REPGSZ 个样本打包上报一包 S6F1；TOTSMP 达到后 flush 剩余样本并停用。
        /// </summary>
        private sealed class TracePlan : IDisposable
        {
            private readonly long _instanceId;
            private readonly uint _traceId;
            private readonly uint _totSmp;
            private readonly uint _repgsz;
            private readonly IReadOnlyList<int> _svids;
            private readonly SecsTraceService _owner;

            private readonly object _gate = new();
            private System.Threading.Timer? _timer;
            private uint _sampleNo;
            private readonly List<Item> _buffer = new();

            public TracePlan(long instanceId, uint traceId, int periodMs, uint totSmp, uint repgsz,
                IReadOnlyList<int> svids, SecsTraceService owner)
            {
                _instanceId = instanceId;
                _traceId = traceId;
                _totSmp = totSmp;
                _repgsz = repgsz;
                _svids = svids;
                _owner = owner;
                PeriodMs = periodMs;
            }

            public int PeriodMs { get; }

            public void Start()
            {
                lock (_gate)
                {
                    _timer ??= new System.Threading.Timer(
                        _ => _ = TickAsync(),
                        null,
                        PeriodMs,
                        PeriodMs);
                }
            }

            private async Task TickAsync()
            {
                uint no;
                lock (_gate)
                {
                    no = ++_sampleNo;
                }

                try
                {
                    var sample = await _owner.CollectSampleAsync(_instanceId, no, _svids);

                    // 攒入缓冲，达到 REPGSZ 则上报一批
                    Item[]? toSend = null;
                    lock (_gate)
                    {
                        _buffer.Add(sample);
                        if (_buffer.Count >= _repgsz)
                        {
                            toSend = _buffer.ToArray();
                            _buffer.Clear();
                        }
                    }

                    if (toSend != null)
                    {
                        await _owner.SendBatchAsync(_instanceId, _traceId, toSend);
                    }

                    // 达到总采样上限 → flush 剩余样本并停用
                    if (_totSmp > 0 && no >= _totSmp)
                    {
                        lock (_gate)
                        {
                            toSend = _buffer.Count > 0 ? _buffer.ToArray() : null;
                            _buffer.Clear();
                        }
                        if (toSend != null && toSend.Length > 0)
                        {
                            await _owner.SendBatchAsync(_instanceId, _traceId, toSend);
                        }

                        Stop();
                        Log.Information("[SECS] Trace TRID={Trid} 达到总采样数 {TotSmp}，自动停用", _traceId, _totSmp);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[SECS] Trace TRID={Trid} 采样上报异常", _traceId);
                }
            }

            public void Stop()
            {
                lock (_gate)
                {
                    _timer?.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                    _timer?.Dispose();
                    _timer = null;
                }
            }

            public void Dispose() => Stop();
        }
    }
}
