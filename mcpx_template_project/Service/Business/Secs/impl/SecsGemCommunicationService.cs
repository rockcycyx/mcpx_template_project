using Microsoft.Extensions.Options;
using Secs4Net;
using Serilog;
using mcpx_template_project.Domain.Model.Secs;
using SecsGemImpl = Secs4Net.SecsGem;
using mcpx_template_project.Core.Context;
using mcpx_template_project.Service.Business.Secs;
using mcpx_template_project.Controller.SecsGem;
using mcpx_template_project.Core.SecsRoute;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// SECS/GEM 通讯引擎实现：Equipment（Passive）端。
    /// 使用 Secs4Net 库，动态创建连接实例以支持运行时修改配置后重启监听。
    /// </summary>
    public sealed class SecsGemCommunicationService : ISecsGemCommunicationService
    {
        private readonly SecsRouter _router;
        private readonly ISecsContext _secsContext;
        private readonly object _lock = new();
        private ISecsConnection? _connection;
        private SecsGemImpl? _gem;
        private CancellationTokenSource? _cts;
        private Task? _messageLoop;
        private Task? _releaseTask;
        private CancellationTokenSource? _establishCts;

        private bool _communicating;
        private string _stateText = "未启动";
        private ushort _deviceId;

        public SecsGemCommunicationService(SecsRouter router, ISecsContext secsContext)
        {
            _router = router;
            _secsContext = secsContext;
        }

        public long? InstanceId { get; set; }

        public bool IsRunning { get; private set; }
        public bool IsCommunicating
        {
            get
            {
                lock (_lock) return _communicating;
            }
        }
        public string? LastError { get; private set; }
        public string StateText
        {
            get
            {
                lock (_lock) return _stateText;
            }
            private set
            {
                lock (_lock) _stateText = value;
            }
        }
        public string DeviceIpAddress
        {
            get
            {
                lock (_lock)
                {
                    return _connection?.DeviceIpAddress ?? "N/A";
                }
            }
        }

        public event EventHandler? StateChanged;
        public event EventHandler<string>? MessageReceived;

        /// <summary>
        /// 设备端主动向 Host 发送消息（S6F1/S5F1/S9Fx 等上报）。
        /// 仅处于 COMMUNICATING 状态时发送。
        /// </summary>
        public async Task<bool> SendAsync(SecsMessage msg, CancellationToken ct)
        {
            SecsGemImpl? gem;
            lock (_lock)
            {
                gem = _gem;
                if (!_communicating) return false;
            }
            if (gem == null) return false;

            try
            {
                await gem.SendAsync(msg, ct).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SECS] 主动发送消息 S{Stream}F{Function} 失败", msg.S, msg.F);
                return false;
            }
        }

        public Task<bool> StartAsync(SecsGemConfiguration config)
        {
            return StartInternalAsync(config);
        }

        private async Task<bool> StartInternalAsync(SecsGemConfiguration config)
        {
            lock (_lock)
            {
                StopInternal();
            }

            // 等待旧连接释放完成，不再使用 GetResult 同步阻塞
            if (_releaseTask != null)
            {
                try
                {
                    await _releaseTask;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[SECS] 等待旧连接释放时发生异常");
                }
            }

            lock (_lock)
            {
                LastError = null;
                StateText = "启动中";
                try
                {
                    _deviceId = config.DeviceId;
                    var options = new SecsGemOptions
                    {
                        DeviceId = config.DeviceId,
                        IsActive = false,          // Passive 端：本机监听，等待 Host 连接
                        IpAddress = config.IpAddress,
                        Port = config.Port,
                        T3 = config.T3,
                        T5 = config.T5,
                        T6 = config.T6,
                        T7 = config.T7,
                        T8 = config.T8,
                    };
                    var optionsWrapper = Options.Create(options);
                    var logger = new SecsGemLogger();
                    _connection = new HsmsConnection(optionsWrapper, logger);
                    _gem = new SecsGemImpl(optionsWrapper, _connection, logger);
                    _connection.ConnectionChanged += OnConnectionChanged;
                    _cts = new CancellationTokenSource();
                    _connection.Start(_cts.Token);
                    IsRunning = true;
                    StateText = "监听中";
                    // 后台消费主消息并自动回复
                    _messageLoop = Task.Run(() => ProcessPrimaryMessagesAsync(_cts.Token));
                    Log.Information("[SECS] 监听已启动 {Ip}:{Port} (DeviceId={DeviceId})",
                        config.IpAddress, config.Port, config.DeviceId);
                    return true;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    StateText = "启动失败";
                    Log.Error(ex, "[SECS] 监听启动失败");
                    StopInternal();
                    return false;
                }
            }
        }

        private void OnConnectionChanged(object? sender, ConnectionState state)
        {
            // 底层Socket回调线程，不直接触发UI事件，投递到线程池
            _ = Task.Run(() =>
            {
                lock (_lock)
                {
                    StateText = state == ConnectionState.Selected ? "已通信" : state.ToString();
                }
                StateChanged?.Invoke(this, EventArgs.Empty);
                Log.Information("[SECS] 连接状态变化：{State}", state);

                if (state == ConnectionState.Selected)
                {
                    lock (_lock)
                    {
                        if (!_communicating)
                        {
                            CancelEstablishInternal();
                            _establishCts = new CancellationTokenSource();
                            var token = _establishCts.Token;
                            _ = Task.Run(() => EstablishCommunicationAsync(token));
                        }
                    }
                }
                else
                {
                    lock (_lock)
                    {
                        CancelEstablishInternal();
                        _communicating = false;
                    }
                }
            });
        }

        /// <summary>
        /// 【锁内版本】取消握手任务，保护 _establishCts 并发安全
        /// </summary>
        private void CancelEstablishInternal()
        {
            try { _establishCts?.Cancel(); } catch { }
            try { _establishCts?.Dispose(); } catch { }
            _establishCts = null;
        }

        /// <summary>
        /// 对外调用的取消握手，加锁
        /// </summary>
        private void CancelEstablish()
        {
            lock (_lock)
            {
                CancelEstablishInternal();
            }
        }

        /// <summary>
        /// 设备端主动建立通信（SEMI E30 4.1.2）：
        /// 1. 发送 S1F13 W，等待 S1F14（T3 超时视为 Establish Communications Timeout，回到步骤 1）
        /// 2. 若 COMACK != Accept，延时后重新发起
        /// 3. COMACK == Accept → 通信状态置为 COMMUNICATING
        /// </summary>
        private async Task EstablishCommunicationAsync(CancellationToken ct)
        {
            SecsGemImpl? gem;
            lock (_lock)
            {
                gem = _gem;
            }
            if (gem == null) return;

            var attempt = 0;
            while (!ct.IsCancellationRequested)
            {
                lock (_lock)
                {
                    if (_communicating)
                        return;
                }

                attempt++;
                try
                {
                    Log.Information("[SECS] 设备端发起建立通信交握（第 {Attempt} 次）", attempt);
                    using var request = new SecsMessage(1, 13, replyExpected: true)
                    {
                        Name = "EstablishCommunicationsRequest",
                        SecsItem = Item.L() // L,0
                    };
                    // SendAsync 返回对方回复（S1F14），内部受 T3 超时约束
                    using var reply = await gem.SendAsync(request, ct);
                    // S1F14: L,2 [COMMACK B, MDLN L,0]（COMMACK 是第一个子项）
                    byte commack = 0xFF;
                    var data = reply.SecsItem;
                    if (data != null && data.Format == SecsFormat.List && data.Items.Length > 0)
                    {
                        var ackItem = data.Items[0];
                        if ((ackItem.Format == SecsFormat.Binary || ackItem.Format == SecsFormat.U1) && ackItem.Count > 0)
                            commack = ackItem.FirstValue<byte>();
                    }
                    if (commack == 0x00)
                    {
                        lock (_lock)
                        {
                            _communicating = true;
                            StateText = "已通信 (COMMUNICATING)";
                        }
                        StateChanged?.Invoke(this, EventArgs.Empty);
                        Log.Information("[SECS] 建立通信成功：COMACK=Accept，进入 COMMUNICATING 状态");
                        return;
                    }
                    Log.Warning("[SECS] 对方未接受建立通信：COMACK={Commack}，等待后重新发起", commack);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (TimeoutException t3Ex)
                {
                    Log.Warning("[SECS] 建立通信尝试 T3 超时 {Message}，重新发起", t3Ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[SECS] 建立通信尝试失败，重新发起");
                }

                // 重试间隔：建议配置化，这里保留固定延时仅作为占位，生产建议放到SecsGemConfiguration
                try
                {
                    await Task.Delay(3000, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task ProcessPrimaryMessagesAsync(CancellationToken ct)
        {
            SecsGemImpl? gem;
            lock (_lock)
            {
                gem = _gem;
            }
            if (gem == null) return;

            try
            {
                await foreach (var e in gem.GetPrimaryMessageAsync(ct).WithCancellation(ct))
                {
                    using var primary = e.PrimaryMessage;
                    Log.Information("[SECS] 收到主消息 S{S}F{F}: {Msg}", primary.S, primary.F, primary);
                    try
                    {
                        MessageReceived?.Invoke(this, $"S{primary.S}F{primary.F}");

                        // 分发到对应 Controller 方法（类 SpringMVC 路由）
                        SecsMessage? reply = null;
                        bool routed = _router.TryGetRoute(primary.S, primary.F, out _);

                        if (routed)
                        {
                            try
                            {
                                // 多实例上下文注入：分发前把本引擎的实例 ID 写入 AsyncLocal，
                                // Controller 内可通过 ISecsContext.CurrentInstanceId 取到「消息来自哪个实例」
                                if (InstanceId.HasValue)
                                {
                                    using var _ = _secsContext.SetInstance(InstanceId.Value);
                                    reply = await _router.DispatchAsync(primary, ct).ConfigureAwait(false);
                                }
                                else
                                {
                                    reply = await _router.DispatchAsync(primary, ct).ConfigureAwait(false);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "[SECS] S{S}F{F} Controller 执行异常", primary.S, primary.F);
                                // 处理异常回 S9F7 非法数据
                                reply = Secs9ErrorFactory.IllegalData(_deviceId, primary.S, primary.F);
                            }
                        }
                        else if (primary.ReplyExpected)
                        {
                            // 未注册路由：区分 Stream 未知（S9F3）与 Function 未知（S9F5）
                            reply = _router.HasStream(primary.S)
                                ? Secs9ErrorFactory.UnrecognizedFunction(_deviceId, primary.S, primary.F)
                                : Secs9ErrorFactory.UnrecognizedStream(_deviceId, primary.S);
                            Log.Warning("[SECS] 未找到处理器 S{S}F{F}，回 {Name}", primary.S, primary.F,
                                reply.Name);
                        }

                        // S1F13 特殊控制逻辑：对方（Host）也发起建立通信（4.1.3 同时发起场景）
                        // 回复报文构造由 S1Controller.S1F13 负责，此处仅维护引擎通信状态
                        if (primary.S == 1 && primary.F == 13)
                        {
                            lock (_lock)
                            {
                                _communicating = true;
                                CancelEstablishInternal();
                            }
                            StateChanged?.Invoke(this, EventArgs.Empty);
                        }

                        // 有回复则回复给 Host
                        if (reply != null)
                        {
                            await e.TryReplyAsync(reply, ct).ConfigureAwait(false);
                            reply.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "[SECS] 处理主消息 S{S}F{F} 异常", primary.S, primary.F);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停止
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SECS] 主消息处理循环异常");
            }
        }

        public void Stop()
        {
            lock (_lock) StopInternal();
        }

        private void StopInternal()
        {
            // 1. 先取消交握循环
            CancelEstablishInternal();
            _communicating = false;
            // 2. 取消主 token：令消息循环 GetPrimaryMessageAsync 与 SendAsync 退出
            try { _cts?.Cancel(); } catch { }

            // 3. 解绑事件、清空引用
            var connection = _connection;
            var gem = _gem;
            if (connection != null)
            {
                connection.ConnectionChanged -= OnConnectionChanged;
            }
            _connection = null;
            _gem = null;
            _messageLoop = null;
            IsRunning = false;
            StateText = "未启动";
            StateChanged?.Invoke(this, EventArgs.Empty);

            // 4. 锁外异步释放，绝不阻塞调用线程
            _releaseTask = ReleaseAsync(connection, gem, _cts);
            _cts = null;
        }

        /// <summary>
        /// 异步释放连接与 gem，不阻塞调用线程。
        /// HsmsConnection 实现IAsyncDisposable，SecsGem仅支持同步Dispose
        /// </summary>
        private static async Task ReleaseAsync(ISecsConnection? connection, SecsGemImpl? gem, CancellationTokenSource? cts)
        {
            if (connection is IAsyncDisposable asyncDisposableConn)
            {
                try
                {
                    await asyncDisposableConn.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[SECS] HSMS Connection DisposeAsync 异常");
                }
            }
            // SecsGem 不支持IAsyncDisposable，只执行同步Dispose
            try
            {
                gem?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[SECS] SecsGem Dispose 异常");
            }

            try
            {
                cts?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[SECS] CancellationTokenSource Dispose 异常");
            }
            Log.Information("[SECS] 资源释放流程完成");
        }
    }
}
