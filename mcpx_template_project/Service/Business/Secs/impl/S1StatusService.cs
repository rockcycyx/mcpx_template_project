using mcpx_template_project.Domain.Entities.Business.Secs;
using mcpx_template_project.Domain.Enums;
using Serilog;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// Stream 1（设备状态）业务服务实现。
    /// 依据 secs_vid（vid_type=SVID）取当前值/名称，状态查询委托 <see cref="ISecsStateService"/>。
    /// </summary>
    public sealed class S1StatusService : IS1StatusService
    {
        private readonly ISecsVidService _vidService;
        private readonly ISecsInstanceService _instanceService;
        private readonly ISecsValueReader _valueReader;
        private readonly ISecsStateService _stateService;

        public S1StatusService(
            ISecsVidService vidService,
            ISecsInstanceService instanceService,
            ISecsValueReader valueReader,
            ISecsStateService stateService)
        {
            _vidService = vidService;
            _instanceService = instanceService;
            _valueReader = valueReader;
            _stateService = stateService;
        }

        public string ModelName => "mcpx_template_project";

        public string SoftwareRevision => "v1.0.0";

        public async Task<List<SecsValueResult>> GetStatusValuesAsync(long instanceId, IReadOnlyList<int> svids)
        {
            var vids = await _vidService.QueryAllAsync(instanceId, SecsVidType.SVID);

            // 空列表 → 查询全部 SVID
            IEnumerable<SecsVid> target;
            if (svids.Count == 0)
            {
                target = vids;
            }
            else
            {
                var set = svids.ToHashSet();
                // 按请求顺序返回（不存在时也要占位）
                var byId = vids.ToDictionary(v => v.VidId);
                var results = new List<SecsValueResult>(svids.Count);
                foreach (var id in svids)
                {
                    if (byId.TryGetValue(id, out var vid))
                        results.Add(await _valueReader.ReadAsync(instanceId, await GetCommSettingIdAsync(instanceId), vid));
                    else
                    {
                        Log.Warning("[SECS] S1F3/S1F11：SVID {Svid} 在 secs_vid(instance={InstanceId}, type=SVID) 中不存在", id, instanceId);
                        results.Add(new SecsValueResult { VidId = id, Exists = false });
                    }
                }
                return results;
            }

            var list = new List<SecsValueResult>();
            foreach (var vid in target)
            {
                list.Add(await _valueReader.ReadAsync(instanceId, await GetCommSettingIdAsync(instanceId), vid));
            }
            return list;
        }

        public async Task<List<SvidNameUnit>> GetStatusNamesAsync(long instanceId, IReadOnlyList<int> svids)
        {
            var vids = await _vidService.QueryAllAsync(instanceId, SecsVidType.SVID);

            IEnumerable<SecsVid> target = svids.Count == 0
                ? vids
                : vids.Where(v => svids.Contains(v.VidId));

            // 空列表 → 全部；否则按请求顺序（缺省时名称/单位为空）
            if (svids.Count == 0)
            {
                return target.Select(v => new SvidNameUnit
                {
                    Svid = v.VidId,
                    Name = v.Name,
                    Units = v.DataType.ToString(),
                }).ToList();
            }

            var byId = target.ToDictionary(v => v.VidId);
            var list = new List<SvidNameUnit>(svids.Count);
            foreach (var id in svids)
            {
                list.Add(byId.TryGetValue(id, out var v)
                    ? new SvidNameUnit { Svid = id, Name = v.Name, Units = v.DataType.ToString() }
                    : new SvidNameUnit { Svid = id, Name = null, Units = null });
            }
            return list;
        }

        public bool IsOnline(long instanceId)
        {
            var st = _stateService.GetControlState(instanceId);
            return st is SecsControlState.OnlineLocal or SecsControlState.OnlineRemote;
        }

        public SecsControlState GetControlState(long instanceId) => _stateService.GetControlState(instanceId);

        private async Task<long?> GetCommSettingIdAsync(long instanceId)
        {
            var instance = await _instanceService.GetByIdAsync(instanceId);
            return instance?.CommSettingId;
        }
    }
}
