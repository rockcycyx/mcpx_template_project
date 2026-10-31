using System.Globalization;
using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Core.SecsRoute;
using mcpx_template_project.Domain.Entities.Business.Secs;
using SecsReportEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsReport;
using SecsCeidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsCeid;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// Stream 2（设备控制与诊断）业务服务实现。
    /// 常量读/写基于 secs_vid（vid_type=ECID）；Trace 采样下沉到 IS2TraceService；
    /// 报表定义/关联（S2F33/S2F35）与事件使能（S2F37）已落地持久化到 secs_report / secs_ceid 及其子表；
    /// 限值/spooling 相关动作本期为「校验 + 应答」骨架，留待后续接入。
    /// </summary>
    public sealed class S2EquipmentService : IS2EquipmentService
    {
        private readonly ISecsVidService _vidService;
        private readonly ISecsInstanceService _instanceService;
        private readonly ISecsValueReader _valueReader;
        private readonly ISecsStateService _stateService;
        private readonly IS2TraceService _traceService;
        private readonly ISecsReportService _reportService;
        private readonly ISecsCeidService _ceidService;
        private readonly IDbContextFactory<EFDbContext> _contextFactory;
        private readonly HcmdRegistry _hcmdRegistry;

        public S2EquipmentService(
            ISecsVidService vidService,
            ISecsInstanceService instanceService,
            ISecsValueReader valueReader,
            ISecsStateService stateService,
            IS2TraceService traceService,
            ISecsReportService reportService,
            ISecsCeidService ceidService,
            IDbContextFactory<EFDbContext> contextFactory,
            HcmdRegistry hcmdRegistry)
        {
            _vidService = vidService;
            _instanceService = instanceService;
            _valueReader = valueReader;
            _stateService = stateService;
            _traceService = traceService;
            _reportService = reportService;
            _ceidService = ceidService;
            _contextFactory = contextFactory;
            _hcmdRegistry = hcmdRegistry;
        }

        public string CurrentTimeText => DateTime.Now.ToString("yyyyMMddHHmmss");

        public async Task<List<SecsValueResult>> GetConstantValuesAsync(long instanceId, IReadOnlyList<int> ecids)
        {
            var vids = await _vidService.QueryAllAsync(instanceId, SecsVidType.ECID);
            long? commSettingId = (await _instanceService.GetByIdAsync(instanceId))?.CommSettingId;

            if (ecids.Count == 0)
            {
                var all = new List<SecsValueResult>();
                foreach (var v in vids)
                    all.Add(await _valueReader.ReadAsync(instanceId, commSettingId, v));
                return all;
            }

            var byId = vids.ToDictionary(v => v.VidId);
            var list = new List<SecsValueResult>(ecids.Count);
            foreach (var id in ecids)
            {
                list.Add(byId.TryGetValue(id, out var v)
                    ? await _valueReader.ReadAsync(instanceId, commSettingId, v)
                    : new SecsValueResult { VidId = id, Exists = false });
            }
            return list;
        }

        public async Task<List<ConstantNameUnit>> GetConstantNamesAsync(long instanceId, IReadOnlyList<int> ecids)
        {
            var vids = await _vidService.QueryAllAsync(instanceId, SecsVidType.ECID);
            IEnumerable<SecsVid> target = ecids.Count == 0 ? vids : vids.Where(v => ecids.Contains(v.VidId));

            if (ecids.Count == 0)
            {
                return target.Select(v => new ConstantNameUnit
                {
                    Ecid = v.VidId,
                    Name = v.Name,
                    Default = v.DefaultValue,
                    Units = v.DataType.ToString(),
                }).ToList();
            }

            var byId = target.ToDictionary(v => v.VidId);
            var list = new List<ConstantNameUnit>(ecids.Count);
            foreach (var id in ecids)
            {
                list.Add(byId.TryGetValue(id, out var v)
                    ? new ConstantNameUnit { Ecid = id, Name = v.Name, Default = v.DefaultValue, Units = v.DataType.ToString() }
                    : new ConstantNameUnit { Ecid = id });
            }
            return list;
        }

        public async Task<byte> UpdateConstantsAsync(long instanceId, IReadOnlyDictionary<int, string> constants)
        {
            var vids = await _vidService.QueryAllAsync(instanceId, SecsVidType.ECID);
            var byId = vids.ToDictionary(v => v.VidId);

            // 任一 ECID 不存在 → 整体拒绝（S2F16 约定：非零 EAC 则不改动任何 ECID）
            foreach (var id in constants.Keys)
            {
                if (!byId.TryGetValue(id, out _))
                    return SecsAckCodes.EacError;
            }

            foreach (var (id, text) in constants)
            {
                var v = byId[id];
                // 仅 source=db 的常量可写回 default_value；plc 来源写回暂不支持（保守拒绝整体）
                if (v.IsPlcSource)
                    return SecsAckCodes.EacError;

                v.DefaultValue = text;
                await _vidService.UpdateAsync(v);
            }
            return SecsAckCodes.EacOk;
        }

        public byte[] Echo(byte[] data) => data;

        public async Task<byte> InitializeTraceAsync(long instanceId, uint traceId, string dsper, uint totSmp, uint repgsz, IReadOnlyList<int> svids)
        {
            // 采样数据采集：注册/禁用 Trace，后台按 DSPER 周期采样，攒够 REPGSZ 个样本后打包上报 S6F1
            return await _traceService.InitializeAsync(instanceId, traceId, dsper, totSmp, repgsz, svids);
        }

        public async Task<(byte Hcack, IReadOnlyList<(string Name, byte Ack)>? Errors)> ExecuteRemoteCommandAsync(
            long instanceId, string rcmd, IReadOnlyList<(string Name, string Value)> parameters)
        {
            return await ExecuteCommandCoreAsync(instanceId, rcmd, parameters);
        }

        public async Task<(byte Hcack, IReadOnlyList<(string Name, byte Ack)>? Errors)> ExecuteEnhancedCommandAsync(
            long instanceId, string objSpec, string rcmd, IReadOnlyList<(string Name, string Value)> parameters)
        {
            return await ExecuteCommandCoreAsync(instanceId, rcmd, parameters);
        }

        private async Task<(byte Hcack, IReadOnlyList<(string Name, byte Ack)>? Errors)> ExecuteCommandCoreAsync(
            long instanceId, string rcmd, IReadOnlyList<(string Name, string Value)> parameters)
        {
            string command = rcmd.Trim().ToUpperInvariant();

            // 优先查 HCMD 注册表（开发者用 [Hcmd] 注解自定义的命令）
            if (_hcmdRegistry.TryGet(command, out var hcmd))
            {
                var (ok, error) = await _hcmdRegistry.InvokeAsync(instanceId, command, parameters);
                if (ok)
                    return (SecsAckCodes.HcackPerformed, null);

                // 缺参/类型转换失败 → 无效参数；其余（未注册/执行异常）→ 无法执行
                byte ack = error != null && error.StartsWith("缺少参数") || error != null && error.StartsWith("参数 ")
                    ? SecsAckCodes.HcackInvalidParameter
                    : SecsAckCodes.HcackCannotPerformNow;

                var errList = new List<(string Name, byte Ack)> { (command, ack) };
                return (ack, errList);
            }

            byte hcack;
            IReadOnlyList<(string Name, byte Ack)>? errors = null;

            switch (command)
            {
                case "REMOTE":
                    hcack = _stateService.RequestRemote(instanceId)
                        ? SecsAckCodes.HcackPerformed
                        : SecsAckCodes.HcackCannotPerformNow;
                    break;

                case "LOCAL":
                    hcack = _stateService.RequestLocal(instanceId)
                        ? SecsAckCodes.HcackPerformed
                        : SecsAckCodes.HcackCannotPerformNow;
                    break;

                case "ABORT":
                    // 中止处理：本设备无进行中的事务模型，视为已完成
                    hcack = SecsAckCodes.HcackPerformed;
                    break;

                case "PP_SELECT":
                    // 工艺程序选择：依赖 S7 工艺程序存储，本期未接入，返回「命令不存在」
                    hcack = SecsAckCodes.HcackCommandNotExist;
                    break;

                default:
                    hcack = SecsAckCodes.HcackCommandNotExist;
                    break;
            }

            return (hcack, errors);
        }

        public async Task<(byte Rspack, IReadOnlyList<(byte Stream, IReadOnlyList<byte> Functions)>? Errors)> ResetSpoolingAsync(
            long instanceId, IReadOnlyList<(byte Stream, IReadOnlyList<byte> Functions)> config)
        {
            // Spooling 未落地，统一接受
            await Task.CompletedTask;
            return (SecsAckCodes.RspackAccept, null);
        }

        public async Task<byte> DefineLimitAttributesAsync(long instanceId)
        {
            // Limit Monitoring 仅校验不落地
            await Task.CompletedTask;
            return SecsAckCodes.VlaackAccept;
        }

        public async Task<byte> DefineReportAsync(long instanceId, IReadOnlyList<(int Rptid, IReadOnlyList<int> Vids)> reports)
        {
            // 1. 预加载实例下全部 VID，建立「VID 编号 → 内部 id」映射
            var vids = await _vidService.QueryAllAsync(instanceId);
            var vidIdById = vids.ToDictionary(v => v.VidId, v => v.Id);

            using var db = await _contextFactory.CreateDbContextAsync();
            var existing = await db.SecsReports
                .Where(r => r.InstanceId == instanceId)
                .ToDictionaryAsync(r => r.ReportId, r => r.Id);

            foreach (var (rptid, vidNumbers) in reports)
            {
                // 2. 校验所有 VID 编号均存在，否则 DRACK=4（VID 不存在）
                var vidInternalIds = new List<long>(vidNumbers.Count);
                foreach (var vidNumber in vidNumbers)
                {
                    if (!vidIdById.TryGetValue(vidNumber, out var vidInternalId))
                        return SecsAckCodes.DrackVidNotExist;
                    vidInternalIds.Add(vidInternalId);
                }

                if (existing.TryGetValue(rptid, out var reportInternalId))
                {
                    // 已存在 → 覆盖其 VID 成员明细
                    var reportEntity = await db.SecsReports
                        .Include(r => r.Vids)
                        .FirstOrDefaultAsync(r => r.Id == reportInternalId);
                    if (reportEntity != null)
                    {
                        db.SecsReportVids.RemoveRange(reportEntity.Vids);
                        reportEntity.Vids = BuildReportVids(vidInternalIds);
                        reportEntity.UpdateTime = DateTime.Now;
                    }
                }
                else
                {
                    // 不存在 → 新增
                    db.SecsReports.Add(new SecsReportEntity
                    {
                        InstanceId = instanceId,
                        ReportId = rptid,
                        Name = rptid.ToString(),
                        Status = "0",
                        CreateTime = DateTime.Now,
                        Vids = BuildReportVids(vidInternalIds),
                    });
                }
            }

            await db.SaveChangesAsync();
            return SecsAckCodes.DrackAccept;
        }

        public async Task<byte> LinkEventReportAsync(long instanceId, IReadOnlyList<(int Ceid, IReadOnlyList<int> Rptids)> links)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            // 预加载实例下全部 Report，建立「RPTID → 内部 id」映射
            var reportIdByRptid = await db.SecsReports
                .Where(r => r.InstanceId == instanceId)
                .ToDictionaryAsync(r => r.ReportId, r => r.Id);

            var ceidByNumber = await db.SecsCeids
                .Where(c => c.InstanceId == instanceId)
                .Include(c => c.Reports)
                .ToDictionaryAsync(c => c.Ceid, c => c);

            foreach (var (ceid, rptids) in links)
            {
                // 校验所有 RPTID 均存在，否则 LRACK=5（RPTID 不存在）
                var reportInternalIds = new List<long>(rptids.Count);
                foreach (var rptid in rptids)
                {
                    if (!reportIdByRptid.TryGetValue(rptid, out var reportInternalId))
                        return SecsAckCodes.LrackRptidNotExist;
                    reportInternalIds.Add(reportInternalId);
                }

                if (ceidByNumber.TryGetValue(ceid, out var ceidEntity))
                {
                    // 已存在 → 覆盖其 Report 成员明细
                    db.SecsCeidReports.RemoveRange(ceidEntity.Reports);
                    ceidEntity.Reports = BuildCeidReports(reportInternalIds);
                    ceidEntity.UpdateTime = DateTime.Now;
                }
                else
                {
                    // 不存在 → 自动建立 CEID（关联事件报表时隐式创建收集事件）
                    var newCeid = new SecsCeidEntity
                    {
                        InstanceId = instanceId,
                        Ceid = ceid,
                        Name = ceid.ToString(),
                        Status = "0",
                        CreateTime = DateTime.Now,
                        Reports = BuildCeidReports(reportInternalIds),
                    };
                    db.SecsCeids.Add(newCeid);
                    ceidByNumber[ceid] = newCeid;
                }
            }

            await db.SaveChangesAsync();
            return SecsAckCodes.LrackAccept;
        }

        public async Task<byte> EnableDisableEventAsync(long instanceId, bool enable, IReadOnlyList<int> ceids)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var targets = ceids.Count == 0
                ? db.SecsCeids.Where(c => c.InstanceId == instanceId).ToList()
                : db.SecsCeids.Where(c => c.InstanceId == instanceId && ceids.Contains(c.Ceid)).ToList();

            // 指定 CEID 时校验存在性，否则 ERACK=1（CEID 不存在）
            if (ceids.Count > 0)
            {
                var found = targets.Select(c => c.Ceid).ToHashSet();
                foreach (var ceid in ceids)
                {
                    if (!found.Contains(ceid))
                        return SecsAckCodes.ErackCeidNotExist;
                }
            }

            string status = enable ? "0" : "1";
            foreach (var ceid in targets)
            {
                ceid.Status = status;
                ceid.UpdateTime = DateTime.Now;
            }

            await db.SaveChangesAsync();
            return SecsAckCodes.ErackAccept;
        }

        /// <summary>按 VID 内部 id 顺序构建 secs_report_vid 成员，sort 从 1 递增。</summary>
        private static List<SecsReportVid> BuildReportVids(IReadOnlyList<long> vidInternalIds)
        {
            var list = new List<SecsReportVid>(vidInternalIds.Count);
            for (int i = 0; i < vidInternalIds.Count; i++)
            {
                list.Add(new SecsReportVid { VidId = vidInternalIds[i], Sort = i + 1 });
            }
            return list;
        }

        /// <summary>按 Report 内部 id 顺序构建 secs_ceid_report 成员，sort 从 1 递增。</summary>
        private static List<SecsCeidReport> BuildCeidReports(IReadOnlyList<long> reportInternalIds)
        {
            var list = new List<SecsCeidReport>(reportInternalIds.Count);
            for (int i = 0; i < reportInternalIds.Count; i++)
            {
                list.Add(new SecsCeidReport { ReportId = reportInternalIds[i], Sort = i + 1 });
            }
            return list;
        }

        public async Task<byte> SetTimeAsync(long instanceId, string timeText)
        {
            // 时间同步：仅接受（设备时间以系统时钟为准），不做真实写回
            await Task.CompletedTask;
            return SecsAckCodes.TiackOk;
        }
    }
}
