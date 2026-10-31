using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mcpx_template_project.Domain.Enums
{
    /// <summary>
    /// 设备控制状态（GEM Control State Model）。
    /// 取值对齐 SEMI E30 / 文档第 3.2 节。
    /// </summary>
    public enum SecsControlState
    {
        /// <summary>设备离线</summary>
        EquipmentOffline = 1,

        /// <summary>尝试在线（过渡态）</summary>
        AttemptOnline = 2,

        /// <summary>Host 离线</summary>
        HostOffline = 3,

        /// <summary>在线-本地</summary>
        OnlineLocal = 4,

        /// <summary>在线-远程</summary>
        OnlineRemote = 5,
    }
}
