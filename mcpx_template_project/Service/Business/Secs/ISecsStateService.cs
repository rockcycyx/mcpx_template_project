using mcpx_template_project.Domain.Enums;
using System.Collections.Concurrent;

namespace mcpx_template_project.Service.Business.Secs
{
    /// <summary>
    /// 设备控制状态机服务（GEM Control State Model + 通信建立状态）。
    /// S1F13（建立通信）、S1F15（请求离线）、S1F17（请求在线）、S2F41（REMOTE/LOCAL 命令）
    /// 都会驱动此状态机；S1F1/S1F3/S1F4 等查询据此回报。
    ///
    /// 状态按 instanceId 隔离（多实例），内存态、不落库。进程重启即重置为 EquipmentOffline。
    /// </summary>
    public interface ISecsStateService
    {
        /// <summary>获取指定实例当前控制状态；实例首次访问时初始化为 EquipmentOffline。</summary>
        SecsControlState GetControlState(long instanceId);

        /// <summary>尝试从任意状态切换到指定状态。</summary>
        /// <returns>true=切换成功；false=不允许的转换（如 Offline 下请求 OnlineLocal）</returns>
        bool TrySetControlState(long instanceId, SecsControlState target);

        /// <summary>请求切换到在线本地（S2F41 LOCAL 命令）：仅 OnlineRemote/HostOffline 下允许。</summary>
        bool RequestLocal(long instanceId);

        /// <summary>请求切换到在线远程（S2F41 REMOTE 命令）：仅 OnlineLocal 下允许。</summary>
        bool RequestRemote(long instanceId);

        /// <summary>请求离线（S1F15）：任意在线态可切到 EquipmentOffline。</summary>
        bool RequestOffline(long instanceId);

        /// <summary>请求在线（S1F17）：EquipmentOffline/HostOffline 下允许，进入 OnlineLocal。</summary>
        bool RequestOnline(long instanceId);

        /// <summary>标记通信已建立（S1F13 成功），通信态独立于控制态。</summary>
        void MarkCommunicating(long instanceId);

        /// <summary>清除通信建立标记（连接断开时）。</summary>
        void MarkNotCommunicating(long instanceId);

        /// <summary>指定实例是否已建立通信（COMMUNICATING）。</summary>
        bool IsCommunicating(long instanceId);
    }
}
