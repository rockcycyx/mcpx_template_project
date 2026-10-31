using mcpx_template_project.Domain.Model.Secs;
using mcpx_template_project.Service.Business.Secs;
using Secs4Net;
using Serilog;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// SECS/GEM 多设备管理器实现。
    /// 每台设备一个 <see cref="ISecsGemCommunicationService"/> 引擎实例，用 deviceKey 隔离。
    /// 引擎工厂由 DI 注入（每次返回全新实例，保证连接/状态互不干扰）。
    /// </summary>
    public sealed class SecsGemManager : ISecsGemManager
    {
        private readonly Func<ISecsGemCommunicationService> _engineFactory;
        private readonly object _lock = new();
        private readonly Dictionary<string, ISecsGemCommunicationService> _engines = new(StringComparer.OrdinalIgnoreCase);

        public SecsGemManager(Func<ISecsGemCommunicationService> engineFactory)
        {
            _engineFactory = engineFactory;
        }

        public IReadOnlyDictionary<string, ISecsGemCommunicationService> Engines
        {
            get
            {
                lock (_lock) return new Dictionary<string, ISecsGemCommunicationService>(_engines);
            }
        }

        public ISecsGemCommunicationService? GetEngine(string deviceKey)
        {
            lock (_lock) return _engines.TryGetValue(deviceKey, out var e) ? e : null;
        }

        public async Task<bool> StartAsync(string deviceKey, SecsGemConfiguration config)
        {
            ISecsGemCommunicationService engine;
            lock (_lock)
            {
                if (!_engines.TryGetValue(deviceKey, out engine!))
                {
                    engine = _engineFactory();
                    _engines[deviceKey] = engine;
                }
            }

            // 多实例场景：deviceKey 即 instanceId，写入引擎，供消息分发时注入上下文
            if (long.TryParse(deviceKey, out var instanceId))
                engine.InstanceId = instanceId;

            return await engine.StartAsync(config).ConfigureAwait(false);
        }

        public void Stop(string deviceKey)
        {
            ISecsGemCommunicationService? engine;
            lock (_lock)
            {
                if (_engines.TryGetValue(deviceKey, out engine))
                {
                    _engines.Remove(deviceKey);
                }
            }

            engine?.Stop();
            Log.Information("[SECS] 已停止设备 {DeviceKey} 的监听", deviceKey);
        }

        public void StopAll()
        {
            List<ISecsGemCommunicationService> engines;
            lock (_lock)
            {
                engines = _engines.Values.ToList();
                _engines.Clear();
            }

            foreach (var e in engines)
            {
                try { e.Stop(); } catch (Exception ex) { Log.Warning(ex, "[SECS] 停止设备引擎异常"); }
            }
        }

        public async Task<bool> SendAsync(string deviceKey, SecsMessage msg, CancellationToken ct)
        {
            var engine = GetEngine(deviceKey);
            if (engine is null) return false;
            return await engine.SendAsync(msg, ct).ConfigureAwait(false);
        }

        public bool IsRunning(string deviceKey) => GetEngine(deviceKey)?.IsRunning ?? false;

        public bool IsCommunicating(string deviceKey) => GetEngine(deviceKey)?.IsCommunicating ?? false;
    }
}
