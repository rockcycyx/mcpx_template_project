using mcpx_template_project.Domain.Entities.Business.Secs;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// Stream 6（数据采集）业务服务实现。
    /// 事件报表：CEID → 关联 Report（secs_ceid_report）→ 各 Report 的 VID 成员（secs_report_vid）→ 当前值。
    /// 个体报表：RPTID → 直接查 secs_report 的 VID 成员 → 当前值。
    /// </summary>
    public sealed class S6DataCollectionService : IS6DataCollectionService
    {
        private readonly ISecsCeidService _ceidService;
        private readonly ISecsReportService _reportService;
        private readonly ISecsInstanceService _instanceService;
        private readonly ISecsValueReader _valueReader;

        public S6DataCollectionService(
            ISecsCeidService ceidService,
            ISecsReportService reportService,
            ISecsInstanceService instanceService,
            ISecsValueReader valueReader)
        {
            _ceidService = ceidService;
            _reportService = reportService;
            _instanceService = instanceService;
            _valueReader = valueReader;
        }

        public async Task<EventReportData?> GetEventReportAsync(long instanceId, int ceid)
        {
            var ceids = await _ceidService.QueryAllAsync(instanceId);
            var ceidEntity = ceids.FirstOrDefault(c => c.Ceid == ceid);
            if (ceidEntity == null) return null;

            // 加载所有 Report（含 VID 成员），按主键 id 建索引
            // 注：secs_ceid_report.report_id 关联 secs_report.id（主键），非业务编号 report_id
            var reports = await _reportService.QueryAllAsync(instanceId);
            var reportById = reports.Where(r => r.Status == "0").ToDictionary(r => r.Id);

            long? commSettingId = (await _instanceService.GetByIdAsync(instanceId))?.CommSettingId;

            var result = new EventReportData { DataId = 0, Ceid = ceid };
            foreach (var link in ceidEntity.Reports.OrderBy(r => r.Sort))
            {
                if (!reportById.TryGetValue(link.ReportId, out var report)) continue;

                var values = new List<SecsValueResult>();
                foreach (var rv in report.Vids.OrderBy(v => v.Sort))
                {
                    if (rv.Vid == null) continue;
                    values.Add(await _valueReader.ReadAsync(instanceId, commSettingId, rv.Vid));
                }
                result.Reports.Add((report.ReportId, values));
            }

            return result;
        }

        public async Task<List<SecsValueResult>> GetIndividualReportAsync(long instanceId, int rptid)
        {
            var reports = await _reportService.QueryAllAsync(instanceId);
            var report = reports.FirstOrDefault(r => r.ReportId == rptid && r.Status == "0");
            if (report == null) return new List<SecsValueResult>();

            long? commSettingId = (await _instanceService.GetByIdAsync(instanceId))?.CommSettingId;

            var values = new List<SecsValueResult>();
            foreach (var rv in report.Vids.OrderBy(v => v.Sort))
            {
                if (rv.Vid == null) continue;
                values.Add(await _valueReader.ReadAsync(instanceId, commSettingId, rv.Vid));
            }
            return values;
        }

        public async Task<byte> RequestSpooledDataAsync(long instanceId, bool transmit)
        {
            // Spooling 未落地，统一回「无 spooled 数据」
            await Task.CompletedTask;
            return SecsAckCodes.RsdaNoSpooledData;
        }
    }
}
