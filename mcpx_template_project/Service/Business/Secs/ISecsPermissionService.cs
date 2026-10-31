using mcpx_template_project.Domain.Enums;
using SecsSxFyEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsSxFy;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// SECS 消息执行权限校验服务：判断「作为 Passive 端，某 SxF 在当前控制状态下是否允许执行」。
    /// 权限来源单一：secs_sxfy 表（白名单）。
    ///   - 表中存在启用条目 → 按该条目的状态位判定；
    ///   - 表中无条目（或停用）→ 默认拦截（拒绝执行）。
    /// [SecsPermission] 注解不参与本服务的判定，仅作为路由层的「是否启用前置校验」开关。
    /// </summary>
    public interface ISecsPermissionService
    {
        /// <summary>
        /// 校验指定 (stream, function) 在给定实例的控制状态下是否允许执行。
        /// </summary>
        /// <param name="stream">消息 Stream</param>
        /// <param name="function">消息 Function</param>
        /// <param name="instanceId">SECS 实例 ID</param>
        /// <param name="state">当前控制状态</param>
        /// <returns>true=允许执行；false=当前状态禁止执行（含表中无启用条目）</returns>
        Task<bool> IsAllowedAsync(byte stream, byte function, long instanceId, SecsControlState state);

        /// <summary>
        /// 查询指定 (stream, function) 在 secs_sxfy 表中的权限条目（用于 UI 展示）。
        /// </summary>
        Task<SecsSxFyEntity?> GetAsync(long instanceId, byte stream, byte function);
    }
}
