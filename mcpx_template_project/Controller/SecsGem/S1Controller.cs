using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Core.Context;
using mcpx_template_project.Service.Business.Secs;
using Secs4Net;

namespace mcpx_template_project.Controller.SecsGem
{
    /// <summary>
    /// Stream 1（设备状态）消息处理器。
    /// 职责：解析并校验入参结构（SecsItem），合法则转发 <see cref="IS1StatusService"/> / <see cref="ISecsStateService"/>，
    /// 再组装 SecsMessage 回复。业务动作（取值/切状态）全部下沉到 Service。
    /// </summary>
    [SecsStream(1)]
    public class S1Controller : ISecsController
    {
        private readonly IS1StatusService _statusService;
        private readonly ISecsStateService _stateService;
        private readonly ISecsContext _secsContext;

        public S1Controller(
            IS1StatusService statusService,
            ISecsStateService stateService,
            ISecsContext secsContext)
        {
            _statusService = statusService;
            _stateService = stateService;
            _secsContext = secsContext;
        }

        private long InstanceId => _secsContext.CurrentInstanceId ?? 0;

        /// <summary>S1F1 Are You There Request → S1F2 On Line Data（L,2 [MDLN, SOFTREV]）</summary>
        [SecsFunction(1)]
        public async Task<SecsMessage?> S1F1(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S1F1 结构：Header only
            return await Task.FromResult(new SecsMessage(1, 2, replyExpected: false)
            {
                Name = "OnLineData",
                SecsItem = Item.L(Item.A(_statusService.ModelName), Item.A(_statusService.SoftwareRevision))
            });
        }

        /// <summary>S1F3</summary>
        [SecsFunction(3)]
        [SecsPermission]
        public async Task<SecsMessage?> S1F3(SecsMessage primaryMsg, CancellationToken ct)
        {
            // 零长度列表表示查询全部
            var data = primaryMsg.SecsItem;
            if (data != null && data.Format != SecsFormat.List)
                return null; // 结构非法 → 由调用方回 S9F7

            var svids = ParseU4List(data);

            var values = await _statusService.GetStatusValuesAsync(InstanceId, svids);

            // S1F4 结构：L,n [SV...]，不存在用空列表项占位
            var items = values.Select(v => v.Exists && v.Value != null ? v.Value! : Item.L()).ToArray();
            return new SecsMessage(1, 4, replyExpected: false)
            {
                Name = "SelectedEquipmentStatusData",
                SecsItem = Item.L(items)
            };
        }

        /// <summary>S1F11 Status Variable Name List Request → S1F12 Status Variable name List Reply</summary>
        [SecsFunction(11)]
        [SecsPermission]
        public async Task<SecsMessage?> S1F11(SecsMessage primaryMsg, CancellationToken ct)
        {
            var data = primaryMsg.SecsItem;
            if (data != null && data.Format != SecsFormat.List)
                return null;

            var svids = ParseU4List(data);
            var names = await _statusService.GetStatusNamesAsync(InstanceId, svids);

            // S1F12 结构：L,n [L,3 [SVID, SVNAME, UNITS]]；不存在时 SVNAME/UNITS 为空 ASCII
            var items = names.Select(n => Item.L(
                Item.U4((uint)n.Svid),
                Item.A(n.Name ?? string.Empty),
                Item.A(n.Units ?? string.Empty))).ToArray();

            return new SecsMessage(1, 12, replyExpected: false)
            {
                Name = "StatusVariableNameListReply",
                SecsItem = Item.L(items)
            };
        }

        /// <summary>S1F13 Establish Communications Request → S1F14 Acknowledge（COMMACK + MDLN/SOFTREV）</summary>
        [SecsFunction(13)]
        [SecsPermission]
        public async Task<SecsMessage?> S1F13(SecsMessage primaryMsg, CancellationToken ct)        {
            // S1F13 结构：L,2 [MDLN, SOFTREV]（Host 侧可发零长度列表）
            var data = primaryMsg.SecsItem;
            if (data != null && data.Format != SecsFormat.List)
                return null;

            // 标记通信建立（由通讯引擎 S1F13 特殊分支协同维护 COMMUNICATING，这里更新状态机）
            _stateService.MarkCommunicating(InstanceId);

            // S1F14 = L,2 [COMMACK B(0x00=Accept), L,2 [MDLN, SOFTREV]]
            return await Task.FromResult(new SecsMessage(1, 14, replyExpected: false)
            {
                Name = "EstablishCommunicationsAcknowledge",
                SecsItem = Item.L(
                    Item.B(SecsAckCodes.CommackAccept),
                    Item.L(Item.A(_statusService.ModelName), Item.A(_statusService.SoftwareRevision)))
            });
        }

        /// <summary>S1F15 Request OFF-LINE → S1F16 OFF-LINE Acknowledge</summary>
        [SecsFunction(15)]
        [SecsPermission]
        public async Task<SecsMessage?> S1F15(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S1F15 结构：Header only
            _stateService.RequestOffline(InstanceId);

            // S1F16 = OFLACK B（0=确认）
            return await Task.FromResult(new SecsMessage(1, 16, replyExpected: false)
            {
                Name = "OffLineAcknowledge",
                SecsItem = Item.B(SecsAckCodes.OflackAccept)
            });
        }

        /// <summary>S1F17 Request ON-LINE → S1F18 ON-LINE Acknowledge</summary>
        [SecsFunction(17)]
        [SecsPermission]
        public async Task<SecsMessage?> S1F17(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S1F17 结构：Header only
            bool ok = _stateService.RequestOnline(InstanceId);

            // S1F18 = ONLACK B（0=接受，1=不允许，2=已在线）
            byte ack = ok ? SecsAckCodes.OnlackAccepted
                : (_statusService.IsOnline(InstanceId) ? SecsAckCodes.OnlackAlreadyOnline : SecsAckCodes.OnlackNotAllowed);

            return await Task.FromResult(new SecsMessage(1, 18, replyExpected: false)
            {
                Name = "OnLineAcknowledge",
                SecsItem = Item.B(ack)
            });
        }

        /// <summary>从 List 解析 U4 列表（SVID）。data 为 null 或空列表 → 返回空列表表示「全部」。</summary>
        private static IReadOnlyList<int> ParseU4List(Item? data)
        {
            if (data == null || data.Format != SecsFormat.List) return Array.Empty<int>();
            var list = new List<int>();
            foreach (var item in data.Items)
            {
                switch (item.Format)
                {
                    case SecsFormat.U4: list.Add((int)item.FirstValue<uint>()); break;
                    case SecsFormat.U2: list.Add(item.FirstValue<ushort>()); break;
                    case SecsFormat.U1: list.Add(item.FirstValue<byte>()); break;
                    case SecsFormat.I4: list.Add(item.FirstValue<int>()); break;
                    case SecsFormat.I2: list.Add(item.FirstValue<short>()); break;
                    case SecsFormat.I1: list.Add(item.FirstValue<sbyte>()); break;
                    // List 或其它格式跳过（宽松兼容，不抛异常）
                }
            }
            return list;
        }
    }
}
