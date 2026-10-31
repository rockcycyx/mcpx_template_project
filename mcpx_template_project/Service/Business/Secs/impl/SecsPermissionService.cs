using mcpx_template_project.Domain.Enums;
using Serilog;
using SecsSxFyEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsSxFy;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// SECS 消息执行权限校验服务实现（表驱动白名单）。
    /// 校验规则：查 secs_sxfy 表，存在启用条目 → 按条目状态位判定；否则默认拦截。
    /// [SecsPermission] 注解不参与判定，仅由路由层用作「是否启用前置校验」的开关。
    /// </summary>
    public sealed class SecsPermissionService : ISecsPermissionService
    {
        private readonly ISecsSxFyService _sxFyService;

        public SecsPermissionService(ISecsSxFyService sxFyService)
        {
            _sxFyService = sxFyService;
        }

        public async Task<bool> IsAllowedAsync(
            byte stream,
            byte function,
            long instanceId,
            SecsControlState state)
        {
            // 查表：仅启用条目（Status == "0"）参与判定
            var entry = await _sxFyService.GetByStreamFunctionAsync(instanceId, stream, function);
            if (entry is null || entry.Status != "0")
            {
                Log.Warning("[SECS] 权限拒绝：S{S}F{F} 无启用权限条目（当前控制状态 {State}），默认拦截",
                    stream, function, state);
                return false;
            }

            bool allowed = IsAllowedByEntry(entry, state);
            if (!allowed)
            {
                Log.Warning("[SECS] 权限拒绝：S{S}F{F} 在当前控制状态 {State} 下不允许执行", stream, function, state);
            }
            return allowed;
        }

        public async Task<SecsSxFyEntity?> GetAsync(long instanceId, byte stream, byte function)
            => await _sxFyService.GetByStreamFunctionAsync(instanceId, stream, function);

        private static bool IsAllowedByEntry(SecsSxFyEntity entry, SecsControlState state)
        {
            return state switch
            {
                SecsControlState.EquipmentOffline => entry.AllowOffline,
                SecsControlState.OnlineLocal => entry.AllowOnlineLocal,
                SecsControlState.OnlineRemote => entry.AllowOnlineRemote,
                // 过渡态（AttemptOnline/HostOffline）从严：视为离线
                _ => entry.AllowOffline,
            };
        }
    }
}
