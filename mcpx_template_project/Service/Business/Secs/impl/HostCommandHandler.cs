using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Controller.SecsGem;
using mcpx_template_project.Domain.Common;
using Serilog;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// Host 指令（HCMD）处理器：用 [Hcmd] 注解声明可被 Host 通过 S2F41 调用的命令。
    /// 命令名默认取方法名（转大写）；方法参数名即 S2F41 参数列表中的 CPNAME。
    ///
    /// 示例（对应 S2F41 W L,2 [<A "PP_SELECT">, L,4 [L,2 [<A "EquipID">, <EQID>] ...]]]）：
    ///   [Hcmd]
    ///   public async Task PP_SELECT(HcmdContext ctx, string EquipID, string PPID, string LotID, string WaferCnt) { ... }
    /// </summary>
    public sealed class HostCommandHandler : IHostCommandHandler
    {
        /// <summary>
        /// PP_SELECT 工艺程序选择命令。
        /// 参数：EquipID（设备 ID）、PPID（工艺程序 ID）、LotID（批次 ID 列表）、WaferCnt（晶圆数量列表）。
        /// </summary>
        [Hcmd]
        public async Task PP_SELECT(HcmdContext ctx, string EquipID, string PPID, string LotID, string WaferCnt)
        {
            Log.Information(
                "[HCMD] 执行 PP_SELECT：InstanceId={InstanceId}, EquipID={EquipID}, PPID={PPID}, LotID={LotID}, WaferCnt={WaferCnt}",
                ctx.InstanceId, EquipID, PPID, LotID, WaferCnt);

            // 此处接入真实业务：加载配方、校验晶圆数量等。
            // 示例仅记录日志，开发者在此实现具体逻辑。
            await Task.CompletedTask;
        }
    }
}
