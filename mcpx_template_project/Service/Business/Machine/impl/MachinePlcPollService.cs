using mcpx_template_project.Core.Plc;
using mcpx_template_project.Core.Logging;
using mcpx_template_project.Domain.Enums;
using mcpx_template_project.Domain.Model.Plc;
using mcpx_template_project.Service.Business.Plc;
using MachinePlcEntity = mcpx_template_project.Domain.Entities.Business.Machine.MachinePlc;

namespace mcpx_template_project.Service.Business.Machine.impl
{
    /// <summary>
    /// 机台 PLC 后台轮询服务：唯一的 PLC 扫描入口（针对机台点位）。
    /// 周期性地对每台机台对应的通讯设定，读取机台类 <see cref="MachinePlcEntity"/>
    /// 声明的所有读点位，并把结果回填通讯缓存。
    ///
    /// 读取策略：
    ///   - 位/字设备（int、枚举）：用 <see cref="IPlcCommunicationService.ReadSinglePointAsync"/>；
    ///   - 浮点点位（float/double）：用 <see cref="IPlcCommunicationService.ReadTypedPointAsync{T}"/> 读浮点值。
    /// 全部结果通过 <see cref="IPlcCommunicationService.SetCachedValue"/> 回填缓存，
    /// 使首页 ViewModel 只需「从缓存读」，绝不主动向 PLC 发请求。
    /// </summary>
    public class MachinePlcPollService : IDisposable
    {
        private readonly IMachineService _machineService;
        private readonly IPlcCommunicationService _plcService;
        private readonly ILogService? _logService;

        private readonly object _gate = new();
        private System.Threading.Timer? _timer;
        private bool _running;
        private bool _polling;

        /// <summary>轮询周期（毫秒）</summary>
        private const int PollIntervalMs = 1000;

        /// <summary>机台类的读写计划（注解扫描，缓存一次）</summary>
        private static readonly PlcPointAccessor<MachinePlcEntity> Accessor = new();

        /// <summary>机台类的读点定义（含类型，用于区分浮点/整型读取）</summary>
        private static readonly IReadOnlyList<PlcReadDefinition> ReadDefs = Accessor.Plan.Reads;

        public MachinePlcPollService(
            IMachineService machineService,
            IPlcCommunicationService plcService,
            ILogService? logService = null)
        {
            _machineService = machineService;
            _plcService = plcService;
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
                if (_polling) return;
                _polling = true;
            }

            try
            {
                var machines = await _machineService.QueryAllAsync();

                foreach (var machine in machines)
                {
                    var settingId = machine.CommSettingId;
                    if (!_plcService.IsConnected(settingId))
                        continue;

                    foreach (var def in ReadDefs)
                    {
                        await ReadAndCacheAsync(settingId, def);
                    }
                }
            }
            catch (Exception ex)
            {
                _logService?.LogError(ex, "[机台PLC] 轮询异常");
            }
            finally
            {
                lock (_gate)
                    _polling = false;
            }
        }

        /// <summary>读取单个机台点位并回填缓存（按类型区分浮点/整型读取）。</summary>
        private async Task ReadAndCacheAsync(long settingId, PlcReadDefinition def)
        {
            var code = def.DeviceCode.ToString();
            string? text;

            if (def.DataType == typeof(float) || def.DataType == typeof(double))
            {
                // 浮点：类型化读取，转文本（保留小数，避免科学计数法）
                var value = await _plcService.ReadTypedPointAsync<float>(settingId, code, def.Address);
                text = value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                // 整型/枚举：普通单点读取
                text = await _plcService.ReadSinglePointAsync(settingId, code, def.Address);
                if (text == null) return;
            }

            _plcService.SetCachedValue(settingId, code, def.Address, text);
        }

        public void Dispose()
        {
            Stop();
            _timer?.Dispose();
            _timer = null;
        }
    }
}
