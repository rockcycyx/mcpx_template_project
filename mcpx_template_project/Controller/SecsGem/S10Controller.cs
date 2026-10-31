using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Core.Context;
using mcpx_template_project.Service.Business.Secs;
using Secs4Net;

namespace mcpx_template_project.Controller.SecsGem
{
    /// <summary>
    /// Stream 10（终端服务）消息处理器。
    /// 职责：解析并校验入参结构，合法则转发 <see cref="IS10TerminalService"/>，再组装 SecsMessage 回复。
    /// </summary>
    [SecsStream(10)]
    public class S10Controller : ISecsController
    {
        private readonly IS10TerminalService _terminalService;
        private readonly ISecsContext _secsContext;

        public S10Controller(IS10TerminalService terminalService, ISecsContext secsContext)
        {
            _terminalService = terminalService;
            _secsContext = secsContext;
        }

        private long InstanceId => _secsContext.CurrentInstanceId ?? 0;

        /// <summary>S10F3 Terminal Display → S10F4 Terminal Display Acknowledge</summary>
        [SecsFunction(3)]
        [SecsPermission]
        public async Task<SecsMessage?> S10F3(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S10F3 = L,2 [TID B, TEXT A]
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length != 2) return null;

            var tidItem = data.Items[0];
            var textItem = data.Items[1];
            if (tidItem.Format is not (SecsFormat.Binary or SecsFormat.U1 or SecsFormat.Boolean)) return null;
            if (textItem.Format != SecsFormat.ASCII) return null;

            byte tid = tidItem.Format == SecsFormat.Boolean
                ? (byte)(tidItem.FirstValue<bool>() ? 1 : 0)
                : tidItem.FirstValue<byte>();
            string text = textItem.GetString() ?? string.Empty;

            byte ack = await _terminalService.DisplayAsync(InstanceId, tid, text);

            // S10F4 = ACKC10 B
            return new SecsMessage(10, 4, replyExpected: false)
            {
                Name = "TerminalDisplayAcknowledge",
                SecsItem = Item.B(ack)
            };
        }

        /// <summary>S10F5 Terminal Display Multi-Block → S10F6 Multi-block Acknowledge（本项目不支持多块）</summary>
        [SecsFunction(5)]
        public async Task<SecsMessage?> S10F5(SecsMessage primaryMsg, CancellationToken ct)
        {
            // S10F5 = L,2 [TID, L,n [TEXT...]]
            var data = primaryMsg.SecsItem;
            if (data == null || data.Format != SecsFormat.List || data.Items.Length != 2) return null;

            // 多块显示不支持：ACKC10=1（消息将不被显示）
            return await Task.FromResult(new SecsMessage(10, 6, replyExpected: false)
            {
                Name = "MultiBlockAcknowledge",
                SecsItem = Item.B(SecsAckCodes.Ackc10WillNotDisplay)
            });
        }
    }
}
