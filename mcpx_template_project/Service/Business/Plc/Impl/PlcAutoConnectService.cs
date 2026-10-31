using Serilog;
using mcpx_template_project.Service.Business.CommSetting;

namespace mcpx_template_project.Service.Business.Plc.Impl
{
    /// <summary>
    /// PLC 自动连线服务实现。
    /// 扫描 ConnectMode=auto 的通讯设定，调用通讯引擎自动建立连接。
    /// </summary>
    public class PlcAutoConnectService : IPlcAutoConnectService
    {
        private readonly ICommSettingService _commSettingService;
        private readonly IPlcCommunicationService _plcService;

        public PlcAutoConnectService(
            ICommSettingService commSettingService,
            IPlcCommunicationService plcService)
        {
            _commSettingService = commSettingService;
            _plcService = plcService;
        }

        /// <inheritdoc />
        public async Task<int> AutoConnectAllAsync()
        {
            int successCount = 0;

            var all = await _commSettingService.QueryAllAsync();
            var autoSettings = all.Where(s =>
                string.Equals(s.ConnectMode, "auto", StringComparison.OrdinalIgnoreCase));

            foreach (var setting in autoSettings)
            {
                try
                {
                    // 已连接则跳过，避免重复连接
                    if (_plcService.IsConnected(setting.Id))
                    {
                        successCount++;
                        continue;
                    }

                    var ok = await _plcService.ConnectAsync(setting);
                    if (ok)
                    {
                        successCount++;
                        Log.Information("[PLC] 自动连线成功：{Name}(ID:{Id}) → {Ip}:{Port}",
                            setting.Name, setting.Id, setting.Ip, setting.Port);
                    }
                    else
                    {
                        Log.Warning("[PLC] 自动连线失败：{Name}(ID:{Id})，原因：{Error}",
                            setting.Name, setting.Id, _plcService.LastError);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[PLC] 自动连线异常：{Name}(ID:{Id})", setting.Name, setting.Id);
                }
            }

            Log.Information("[PLC] 自动连线完成：共 {Total} 条 auto 设定，成功 {Success} 条",
                autoSettings.Count(), successCount);
            return successCount;
        }
    }
}
