using mcpx_template_project.Configation;
using mcpx_template_project.Service.Business.Secs;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// SECS/GEM 自动开放服务：登录后遍历所有 ConnectMode=auto 且启用的实例，逐个启动 Passive 监听。
    /// 多实例改造：不再依赖 appsettings.json 单例配置，改为从 secs_instance 表读取。
    /// </summary>
    public interface ISecsGemAutoStartService
    {
        /// <summary>
        /// 为所有 auto 模式的启用实例启动监听。
        /// </summary>
        /// <returns>是否至少启动了一个实例</returns>
        Task<bool> AutoStartIfNeededAsync();
    }

    public sealed class SecsGemAutoStartService : ISecsGemAutoStartService
    {
        private readonly ISecsInstanceService _instanceService;
        private readonly ISecsGemManager _manager;

        public SecsGemAutoStartService(
            ISecsInstanceService instanceService,
            ISecsGemManager manager)
        {
            _instanceService = instanceService;
            _manager = manager;
        }

        public async Task<bool> AutoStartIfNeededAsync()
        {
            var instances = await _instanceService.QueryEnabledAsync();

            bool anyStarted = false;
            foreach (var instance in instances)
            {
                if (!instance.IsAuto) continue;

                // 多实例：deviceKey 用实例 ID（稳定且唯一）
                var deviceKey = instance.Id.ToString();
                if (_manager.IsRunning(deviceKey)) continue;

                var config = SecsGemConfigStore.ToConfiguration(instance);
                bool ok = await _manager.StartAsync(deviceKey, config);
                if (ok) anyStarted = true;
            }

            return anyStarted;
        }
    }
}
