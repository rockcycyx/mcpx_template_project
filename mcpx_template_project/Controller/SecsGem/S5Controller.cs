using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Core.Context;
using mcpx_template_project.Service.Business.Secs;
using Secs4Net;

namespace mcpx_template_project.Controller.SecsGem
{
    /// <summary>
    /// Stream 5（报警）消息处理器。
    /// 职责：解析并校验入参结构，合法则转发 <see cref="IS5AlarmService"/>，再组装 SecsMessage 回复。
    /// </summary>
    [SecsStream(5)]
    public class S5Controller : ISecsController
    {
        private readonly IS5AlarmService _alarmService;
        private readonly ISecsContext _secsContext;

        public S5Controller(IS5AlarmService alarmService, ISecsContext secsContext)
        {
            _alarmService = alarmService;
            _secsContext = secsContext;
        }

        private long InstanceId => _secsContext.CurrentInstanceId ?? 0;

        /// <summary>S5F3 Enable/Disable Alarm Send → S5F4 Enable/Disable Alarm Acknowledge</summary>
        [SecsFunction(3)]
        [SecsPermission]
        public async Task<SecsMessage?> S5F3(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S5F3 = L,2 [ALED B, ALID I4]（ALID 零长度表示全部）；部分 Host 省略 ALID 只发 L,1 [ALED]，视为作用于全部。
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length is < 1 or > 2) return null;

            var aledItem = data.Items[0];
            if (aledItem.Format is not (SecsFormat.Binary or SecsFormat.Boolean or SecsFormat.U1)) return null;

            // ALED 为布尔/Binary/U1：值为 0 表示禁用，非 0 表示启用。空 item 视为禁用。
            bool enable = aledItem.Count == 0
                ? false
                : aledItem.Format == SecsFormat.Boolean
                    ? aledItem.FirstValue<bool>()
                    : aledItem.FirstValue<byte>() != 0;

            // ALID：L,2 时取第二个元素；L,1 时缺省，按空列表（全部）处理。
            var alidItem = data.Items.Length == 2 ? data.Items[1] : null;
            var alids = ParseAlidList(alidItem);
            byte ack = await _alarmService.SetAlarmEnabledAsync(InstanceId, enable, alids);

            // S5F4 = ACKC5 B
            return new SecsMessage(5, 4, replyExpected: false)
            {
                Name = "EnableDisableAlarmAcknowledge",
                SecsItem = Item.B(ack)
            };
        }

        /// <summary>S5F5 List Alarms Request → S5F6 List Alarm Data</summary>
        [SecsFunction(5)]
        [SecsPermission]
        public async Task<SecsMessage?> S5F5(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S5F5 = L,n [ALID...]（零长度表示全部）
            var data = primaryMsg.SecsItem;
            if (data != null && data.Format != SecsFormat.List) return null;

            var alids = ParseAlidList(data);
            var alarms = await _alarmService.ListAlarmsAsync(InstanceId, alids, enabledOnly: false);

            // S5F6 = L,n [L,3 [ALCD, ALID, ALTX]]
            var items = alarms.Select(a => Item.L(
                Item.B(a.Alcd),
                Item.I4(a.Alid),
                Item.A(a.Altext))).ToArray();

            return new SecsMessage(5, 6, replyExpected: false)
            {
                Name = "ListAlarmData",
                SecsItem = Item.L(items)
            };
        }

        /// <summary>S5F7 List Enabled Alarms Request → S5F8 List Enabled Alarm Data</summary>
        [SecsFunction(7)]
        [SecsPermission]
        public async Task<SecsMessage?> S5F7(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S5F7 = Header only
            var alarms = await _alarmService.ListAlarmsAsync(InstanceId, Array.Empty<int>(), enabledOnly: true);

            var items = alarms.Select(a => Item.L(
                Item.B(a.Alcd),
                Item.I4(a.Alid),
                Item.A(a.Altext))).ToArray();

            return new SecsMessage(5, 8, replyExpected: false)
            {
                Name = "ListEnabledAlarmData",
                SecsItem = Item.L(items)
            };
        }

        /// <summary>解析 ALID 列表；零长度/非列表 → 空数组（表示全部）。</summary>
        private static IReadOnlyList<int> ParseAlidList(Item? data)
        {
            if (data == null) return Array.Empty<int>();
            if (data.Format != SecsFormat.List)
            {
                // 单个 ALID（I4/U4/U2 标量）
                return data.Format switch
                {
                    SecsFormat.I4 => new[] { data.FirstValue<int>() },
                    SecsFormat.U4 => new[] { (int)data.FirstValue<uint>() },
                    SecsFormat.U2 => new[] { (int)data.FirstValue<ushort>() },
                    _ => Array.Empty<int>(),
                };
            }
            var list = new List<int>();
            foreach (var item in data.Items)
            {
                switch (item.Format)
                {
                    case SecsFormat.I4: list.Add(item.FirstValue<int>()); break;
                    case SecsFormat.U4: list.Add((int)item.FirstValue<uint>()); break;
                    case SecsFormat.U2: list.Add(item.FirstValue<ushort>()); break;
                    case SecsFormat.U1: list.Add(item.FirstValue<byte>()); break;
                }
            }
            return list;
        }
    }
}
