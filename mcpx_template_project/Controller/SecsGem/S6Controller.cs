using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Core.Context;
using mcpx_template_project.Service.Business.Secs;
using Secs4Net;

namespace mcpx_template_project.Controller.SecsGem
{
    /// <summary>
    /// Stream 6（数据采集）消息处理器。
    /// 职责：解析并校验入参结构，合法则转发 <see cref="IS6DataCollectionService"/>，再组装 SecsMessage 回复。
    /// </summary>
    [SecsStream(6)]
    public class S6Controller : ISecsController
    {
        private readonly IS6DataCollectionService _dataService;
        private readonly ISecsContext _secsContext;

        public S6Controller(IS6DataCollectionService dataService, ISecsContext secsContext)
        {
            _dataService = dataService;
            _secsContext = secsContext;
        }

        private long InstanceId => _secsContext.CurrentInstanceId ?? 0;

        /// <summary>S6F15 Event Report Request → S6F16 Event Report Data</summary>
        [SecsFunction(15)]
        public async Task<SecsMessage?> S6F15(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S6F15 = CEID U4（标量）
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format is not (SecsFormat.U4 or SecsFormat.U2 or SecsFormat.U1)) return null;

            int ceid = (int)(data.Format switch
            {
                SecsFormat.U4 => data.FirstValue<uint>(),
                SecsFormat.U2 => data.FirstValue<ushort>(),
                _ => data.FirstValue<byte>(),
            });

            var report = await _dataService.GetEventReportAsync(InstanceId, ceid);

            // S6F16 = L,3 [DATAID, CEID, L,a [L,2 [RPTID, L,b [V...]]...]]
            // CEID 未定义 → 零长度报表列表（空列表）
            var reportItems = new List<Item>();
            if (report != null)
            {
                foreach (var (rptid, values) in report.Reports)
                {
                    var valItems = values.Select(v => v.Exists && v.Value != null ? v.Value! : Item.L()).ToArray();
                    reportItems.Add(Item.L(Item.U4((uint)rptid), Item.L(valItems)));
                }
            }

            return new SecsMessage(6, 16, replyExpected: false)
            {
                Name = "EventReportData",
                SecsItem = Item.L(Item.U4(report?.DataId ?? 0), Item.U4((uint)ceid), Item.L(reportItems.ToArray()))
            };
        }

        /// <summary>S6F19 Individual Report Request → S6F20 Individual Report Data</summary>
        [SecsFunction(19)]
        public async Task<SecsMessage?> S6F19(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S6F19 = RPTID U4（标量）
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format is not (SecsFormat.U4 or SecsFormat.U2 or SecsFormat.U1)) return null;

            int rptid = (int)(data.Format switch
            {
                SecsFormat.U4 => data.FirstValue<uint>(),
                SecsFormat.U2 => data.FirstValue<ushort>(),
                _ => data.FirstValue<byte>(),
            });

            var values = await _dataService.GetIndividualReportAsync(InstanceId, rptid);

            // S6F20 = L,n [V...]（RPTID 未定义 → 零长度列表）
            var items = values.Select(v => v.Exists && v.Value != null ? v.Value! : Item.L()).ToArray();
            return new SecsMessage(6, 20, replyExpected: false)
            {
                Name = "IndividualReportData",
                SecsItem = Item.L(items)
            };
        }

        /// <summary>S6F23 Request Spooled Data → S6F24 Request Spooled Data Acknowledgement Send</summary>
        [SecsFunction(23)]
        public async Task<SecsMessage?> S6F23(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S6F23 = RSDC U1（0=发送，1=清除）
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format is not (SecsFormat.U1 or SecsFormat.Binary)) return null;

            byte rsdc = data.FirstValue<byte>();
            byte ack = await _dataService.RequestSpooledDataAsync(InstanceId, transmit: rsdc == 0);

            // S6F24 = RSDA B
            return new SecsMessage(6, 24, replyExpected: false)
            {
                Name = "RequestSpooledDataAcknowledgementSend",
                SecsItem = Item.B(ack)
            };
        }
    }
}
