using mcpx_template_project.Domain.Model;
using mcpx_template_project.Service.Business.Trigger;
using PlcTriggerEntity = mcpx_template_project.Domain.Entities.Business.Trigger.PlcTrigger;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// Stream 5（报警处理）业务服务实现。
    /// 报警列表取自 plc_trigger（trigger_type=alarm）。ALID 直接使用触发器主键 Id。
    /// 使能/禁用状态落在 plc_trigger.report_alarm（1=上报，0=不上报）。
    /// </summary>
    public sealed class S5AlarmService : IS5AlarmService
    {
        private readonly IPlcTriggerService _triggerService;

        public S5AlarmService(IPlcTriggerService triggerService)
        {
            _triggerService = triggerService;
        }

        public async Task<List<AlarmInfo>> ListAlarmsAsync(long instanceId, IReadOnlyList<int> alids, bool enabledOnly)
        {
            // 报警属于通讯设定层级（plc_trigger 挂在 comm_setting 下），
            // 与 SECS 实例的关联通过 secs_instance.comm_setting_id 建立，此处由调用方实例过滤后传入。
            // 简化：查询全部报警，再按 alids/enabledOnly 过滤。
            var triggers = await _triggerService.QueryAllAsync(new PlcTriggerQuery { TriggerType = "alarm" });

            IEnumerable<PlcTriggerEntity> alarms = triggers.Where(t => t.IsAlarm);
            if (enabledOnly)
                alarms = alarms.Where(t => t.Status == "1");

            if (alids.Count > 0)
            {
                var set = alids.Select(a => (long)a).ToHashSet();
                alarms = alarms.Where(t => set.Contains(t.Id));
            }

            var query = alarms.Select(t => new AlarmInfo
            {
                // ALCD = bit8(发生=1) | bit7-1(类别)。列表场景固定按「已发生」上报，类别取自配置 alarm_category。
                Alcd = BuildAlcd(category: t.AlarmCategory, occurred: true),
                Alid = (int)t.Id,
                Altext = t.Name,
            });
            return query.ToList();
        }

        public async Task<byte> SetAlarmEnabledAsync(long instanceId, bool enable, IReadOnlyList<int> alids)
        {
            var triggers = await _triggerService.QueryAllAsync(new PlcTriggerQuery { TriggerType = "alarm" });
            var alarms = triggers.Where(t => t.IsAlarm);

            IEnumerable<PlcTriggerEntity> target = alids.Count == 0
                ? alarms
                : alarms.Where(t => alids.Contains((int)t.Id));

            foreach (var t in target)
            {
                // S5F3 语义：使能/禁用「报警上报」，只改 report_alarm（1=上报，0=不上报），不影响监控 status。
                t.ReportAlarm = enable ? "1" : "0";
                await _triggerService.UpdateAsync(t);
            }

            return SecsAckCodes.Ackc5Ok;
        }

        /// <summary>
        /// 组装 ALCD 字节：bit8 = 是否发生（1=发生/0=清除），bit7-1 = 警报类别。
        /// 类别超出 0~8 范围时截断到 0~8，避免溢出污染 bit8。
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static byte BuildAlcd(int category, bool occurred)
        {
            var cat = category;
            if (cat < 0) cat = 0;
            if (cat > 8) cat = 8;

            return (byte)((cat << 1) | (occurred ? 0x80 : 0x00));
        }
    }
}
