using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Service.Business.Secs;
using Secs4Net;

namespace mcpx_template_project.Controller.SecsGem
{
    /// <summary>
    /// Stream 7（工艺程序）消息处理器。
    /// 职责：解析并校验入参结构，合法则转发 <see cref="IS7ProcessProgramService"/>，再组装 SecsMessage 回复。
    /// </summary>
    [SecsStream(7)]
    public class S7Controller : ISecsController
    {
        private readonly IS7ProcessProgramService _programService;
        private readonly IS1StatusService _statusService;

        public S7Controller(IS7ProcessProgramService programService, IS1StatusService statusService)
        {
            _programService = programService;
            _statusService = statusService;
        }

        /// <summary>S7F1 Process Program Load Inquire → S7F2 Process Program Load Grant</summary>
        [SecsFunction(1)]
        [SecsPermission]
        public async Task<SecsMessage?> S7F1(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S7F1 = L,2 [PPID A, LENGTH U4]
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length != 2) return null;
            if (data.Items[0].Format != SecsFormat.ASCII) return null;

            string ppid = data.Items[0].GetString() ?? string.Empty;
            byte ppgnt = await _programService.InquireLoadAsync(ppid);

            // S7F2 = PPGNT B
            return new SecsMessage(7, 2, replyExpected: false)
            {
                Name = "ProcessProgramLoadGrant",
                SecsItem = Item.B(ppgnt)
            };
        }

        /// <summary>S7F17 Delete Process Program Send → S7F18 Delete Process Program Acknowledge</summary>
        [SecsFunction(17)]
        [SecsPermission]
        public async Task<SecsMessage?> S7F17(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S7F17 = L,n [PPID...]（零长度=删除全部）
            var data = primaryMsg.SecsItem;
            if (data != null && data.Format != SecsFormat.List) return null;

            var ppids = new List<string>();
            if (data != null && data.Format == SecsFormat.List)
            {
                foreach (var item in data.Items)
                {
                    if (item.Format != SecsFormat.ASCII) return null;
                    ppids.Add(item.GetString() ?? string.Empty);
                }
            }

            byte ack = await _programService.DeletePpidAsync(ppids);

            // S7F18 = ACKC7 B
            return new SecsMessage(7, 18, replyExpected: false)
            {
                Name = "DeleteProcessProgramAcknowledge",
                SecsItem = Item.B(ack)
            };
        }

        /// <summary>S7F19 Current EPPD Request → S7F20 Current EPPD Data</summary>
        [SecsFunction(19)]
        [SecsPermission]
        public async Task<SecsMessage?> S7F19(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S7F19 = Header only
            var ppids = await _programService.GetPpidListAsync();

            // S7F20 = L,n [PPID...]
            var items = ppids.Select(p => Item.A(p)).ToArray();
            return new SecsMessage(7, 20, replyExpected: false)
            {
                Name = "CurrentEPPDData",
                SecsItem = Item.L(items)
            };
        }

        /// <summary>S7F23 Formatted Process Program Send → S7F24 Formatted Process Program Acknowledge</summary>
        [SecsFunction(23)]
        [SecsPermission]
        public async Task<SecsMessage?> S7F23(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S7F23 = L,4 [PPID A, MDLN A, SOFTREV A, L,c [L,2 [CCODE A, L,p [PPARM...]]...]]
            // 本项目约定：每个配方一个 CCODE，参数列表 16 个，顺序见客户文档。
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length != 4) return null;
            if (data.Items[0].Format != SecsFormat.ASCII) return null;

            string ppid = data.Items[0].GetString() ?? string.Empty;

            // 取命令列表 L,c [L,2 [CCODE, L,p [PPARM...]]...]，本项目取第一条命令（一个配方一个 CCODE）
            var cmdList = data.Items[3];
            if (cmdList.Format != SecsFormat.List || cmdList.Items.Length < 1) return null;

            var cmd = cmdList.Items[0];
            if (cmd.Format != SecsFormat.List || cmd.Items.Length != 2) return null;
            if (cmd.Items[0].Format != SecsFormat.ASCII) return null;

            string ccode = cmd.Items[0].GetString() ?? string.Empty;
            var pparmList = cmd.Items[1];
            if (pparmList.Format != SecsFormat.List) return null;

            var pparms = pparmList.Items.ToList();

            byte ack = await _programService.StoreProcessProgramAsync(ppid, ccode, pparms);

            // S7F24 = ACKC7 B
            return new SecsMessage(7, 24, replyExpected: false)
            {
                Name = "FormattedProcessProgramAcknowledge",
                SecsItem = Item.B(ack)
            };
        }

        /// <summary>S7F25 Formatted Process Program Request → S7F26 Formatted Process Program Data</summary>
        [SecsFunction(25)]
        [SecsPermission]
        public async Task<SecsMessage?> S7F25(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S7F25 = PPID A
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.ASCII) return null;

            string ppid = data.GetString() ?? string.Empty;
            var program = await _programService.GetProcessProgramAsync(ppid);

            if (program == null)
            {
                // PPID 未找到 → 回零长度列表（请求被拒绝）
                return new SecsMessage(7, 26, replyExpected: false)
                {
                    Name = "FormattedProcessProgramData",
                    SecsItem = Item.L()
                };
            }

            // S7F26 = L,4 [PPID A, MDLN A, SOFTREV A, L,1 [L,2 [CCODE A, L,16 [参数...]]]]
            var cmdItem = Item.L(
                Item.A(program.Ccode),
                Item.L(program.Params.ToArray()));

            return new SecsMessage(7, 26, replyExpected: false)
            {
                Name = "FormattedProcessProgramData",
                SecsItem = Item.L(
                    Item.A(ppid),
                    Item.A(_statusService.ModelName),
                    Item.A(_statusService.SoftwareRevision),
                    Item.L(cmdItem))
            };
        }
    }
}
