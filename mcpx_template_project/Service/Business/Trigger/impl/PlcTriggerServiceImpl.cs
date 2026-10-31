using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Helper;
using PlcTriggerEntity = mcpx_template_project.Domain.Entities.Business.Trigger.PlcTrigger;

namespace mcpx_template_project.Service.Business.Trigger.impl
{
    /// <summary>
    /// PLC 触发器业务服务实现。
    /// 导入采用「同一 comm_setting_id + device_code + address 即覆盖」的 Upsert 语义。
    /// </summary>
    public class PlcTriggerServiceImpl : IPlcTriggerService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public PlcTriggerServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<PlcTriggerEntity>> QueryAllAsync(PlcTriggerQuery? query = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            IQueryable<PlcTriggerEntity> queryable = db.PlcTriggers.AsNoTracking();

            if (query == null)
                return await queryable.OrderBy(t => t.CommSettingId).ThenBy(t => t.Id).ToListAsync();

            if (!string.IsNullOrWhiteSpace(query.Keyword))
                queryable = queryable.Where(t => EF.Functions.Like(t.Name, $"%{query.Keyword}%"));

            if (query.CommSettingId.HasValue)
                queryable = queryable.Where(t => t.CommSettingId == query.CommSettingId.Value);

            if (!string.IsNullOrWhiteSpace(query.DeviceCode))
                queryable = queryable.Where(t => t.DeviceCode == query.DeviceCode);

            if (!string.IsNullOrWhiteSpace(query.Address))
                queryable = queryable.Where(t => EF.Functions.Like(t.Address, $"%{query.Address}%"));

            if (!string.IsNullOrWhiteSpace(query.TriggerType))
                queryable = queryable.Where(t => t.TriggerType == query.TriggerType);

            if (!string.IsNullOrWhiteSpace(query.TriggerMode))
                queryable = queryable.Where(t => t.TriggerMode == query.TriggerMode);

            if (!string.IsNullOrWhiteSpace(query.ActionKey))
                queryable = queryable.Where(t => t.ActionKey == query.ActionKey);

            if (query.AlarmCategory.HasValue)
                queryable = queryable.Where(t => t.AlarmCategory == query.AlarmCategory.Value);

            return await queryable.OrderBy(t => t.CommSettingId).ThenBy(t => t.Id).ToListAsync();
        }

        public async Task<List<PlcTriggerEntity>> QueryBySettingAsync(long commSettingId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.PlcTriggers.AsNoTracking()
                .Where(t => t.CommSettingId == commSettingId)
                .OrderBy(t => t.Id)
                .ToListAsync();
        }

        public async Task<PlcTriggerEntity?> GetByIdAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.PlcTriggers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<PlcTriggerEntity> InsertAsync(PlcTriggerEntity trigger)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            await EnsureUniqueAsync(db, trigger, null);
            Normalize(trigger);

            trigger.Id = 0;
            trigger.CreateTime = DateTime.Now;
            trigger.CreateBy = AppHelper.GetCurrentUserName();

            db.PlcTriggers.Add(trigger);
            await db.SaveChangesAsync();
            return trigger;
        }

        public async Task<PlcTriggerEntity> UpdateAsync(PlcTriggerEntity trigger)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.PlcTriggers.FindAsync(trigger.Id);
            if (entity == null)
                throw new InvalidOperationException($"触发器不存在（ID: {trigger.Id}）");

            await EnsureUniqueAsync(db, trigger, trigger.Id);
            Normalize(trigger);

            entity.CommSettingId = trigger.CommSettingId;
            entity.DeviceCode = trigger.DeviceCode;
            entity.Address = trigger.Address;
            entity.TriggerType = trigger.TriggerType;
            entity.TriggerMode = trigger.TriggerMode;
            entity.ActionKey = trigger.ActionKey;
            entity.ReportAlarm = trigger.ReportAlarm;
            entity.AlarmCategory = trigger.AlarmCategory;
            entity.Name = trigger.Name;
            entity.Status = trigger.Status;
            entity.Remark = trigger.Remark;
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;

            await db.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.PlcTriggers.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"触发器不存在（ID: {id}）");

            db.PlcTriggers.Remove(entity);
            await db.SaveChangesAsync();
        }

        public async Task SwitchStatusAsync(long id, string status)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.PlcTriggers.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"触发器不存在（ID: {id}）");

            entity.Status = status == "1" ? "1" : "0";
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;
            await db.SaveChangesAsync();
        }

        public async Task UpdateCategoryAsync(long id, int category)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.PlcTriggers.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"触发器不存在（ID: {id}）");

            entity.AlarmCategory = category is >= 0 and <= 8 ? category : 0;
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;
            await db.SaveChangesAsync();
        }

        public async Task<(int inserted, int updated)> ImportAsync(List<PlcTriggerEntity> triggers)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var userName = AppHelper.GetCurrentUserName();
            int inserted = 0, updated = 0;

            foreach (var trigger in triggers)
            {
                Normalize(trigger);

                var existing = await db.PlcTriggers.FirstOrDefaultAsync(t =>
                    t.CommSettingId == trigger.CommSettingId &&
                    t.DeviceCode == trigger.DeviceCode &&
                    t.Address == trigger.Address);

                if (existing == null)
                {
                    trigger.Id = 0;
                    trigger.CreateTime = DateTime.Now;
                    trigger.CreateBy = userName;
                    db.PlcTriggers.Add(trigger);
                    inserted++;
                }
                else
                {
                    existing.TriggerType = trigger.TriggerType;
                    existing.TriggerMode = trigger.TriggerMode;
                    existing.ActionKey = trigger.ActionKey;
                    existing.ReportAlarm = trigger.ReportAlarm;
                    existing.AlarmCategory = trigger.AlarmCategory;
                    existing.Name = trigger.Name;
                    existing.Status = trigger.Status;
                    existing.Remark = trigger.Remark;
                    existing.UpdateBy = userName;
                    existing.UpdateTime = DateTime.Now;
                    updated++;
                }
            }

            await db.SaveChangesAsync();
            return (inserted, updated);
        }

        /// <summary>规范化字段：类型/触发方式/状态统一小写、事件类型清空无效 action_key。</summary>
        private static void Normalize(PlcTriggerEntity trigger)
        {
            trigger.TriggerType = trigger.TriggerType?.Trim().ToLowerInvariant() ?? "alarm";
            trigger.TriggerMode = trigger.TriggerMode?.Trim().ToLowerInvariant() ?? "on";
            trigger.Status = trigger.Status == "0" ? "0" : "1";

            // 非事件类型时清空 action_key
            if (trigger.TriggerType != "event")
                trigger.ActionKey = null;
        }

        /// <summary>校验同一设备下「设备代号 + 地址」不重复（新增/编辑时）。</summary>
        private static async Task EnsureUniqueAsync(EFDbContext db, PlcTriggerEntity trigger, long? excludeId)
        {
            bool conflict = await db.PlcTriggers.AnyAsync(t =>
                t.CommSettingId == trigger.CommSettingId &&
                t.DeviceCode == trigger.DeviceCode &&
                t.Address == trigger.Address &&
                (excludeId == null || t.Id != excludeId.Value));

            if (conflict)
                throw new InvalidOperationException(
                    $"通讯设定 {trigger.CommSettingId} 下已存在点位 {trigger.DeviceCode}{trigger.Address} 的触发器");
        }

        // ============ Excel 导出 ============

        private static List<ExcelColumnConfig<PlcTriggerEntity>> BuildColumns()
        {
            return new List<ExcelColumnConfig<PlcTriggerEntity>>
            {
                new() { Header = "ID", Selector = t => t.Id },
                new() { Header = "CommSettingID(设备ID)", Selector = t => t.CommSettingId, Setter = (t, v) => t.CommSettingId = ToLong(v) },
                new() { Header = "类型(设备代号)", Selector = t => t.DeviceCode, Setter = (t, v) => t.DeviceCode = ToText(v) },
                new() { Header = "点位(地址)", Selector = t => t.Address, Setter = (t, v) => t.Address = ToText(v) },
                new() { Header = "触发器类型(alarm/event)", Selector = t => t.TriggerType, Setter = (t, v) => t.TriggerType = ToEnumText(v, "alarm") },
                new() { Header = "触发方式(on/change)", Selector = t => t.TriggerMode, Setter = (t, v) => t.TriggerMode = ToEnumText(v, "on") },
                new() { Header = "警报类别(0-8)", Selector = t => t.AlarmCategory, Setter = (t, v) => t.AlarmCategory = ToCategory(v) },
                new() { Header = "事件处理器Key", Selector = t => t.ActionKey, Setter = (t, v) => t.ActionKey = ToNullableText(v) },
                new() { Header = "触发名/警报名", Selector = t => t.Name, Setter = (t, v) => t.Name = ToText(v) },
                new() { Header = "状态(1启用/0停用)", Selector = t => t.Status, Setter = (t, v) => t.Status = ToStatus(v) },
                new() { Header = "Remark", Selector = t => t.Remark, Setter = (t, v) => t.Remark = ToNullableText(v) },
            };
        }

        public void Export(string filePath, IEnumerable<PlcTriggerEntity>? data = null)
        {
            var list = data?.ToList() ?? QueryAllAsync().GetAwaiter().GetResult();
            ExcelHelper.ExportToExcel(list, filePath, BuildColumns());
        }

        public void ExportTemplate(string filePath)
        {
            ExcelHelper.ExportToExcel(new List<PlcTriggerEntity>(), filePath, BuildColumns());
        }

        public List<PlcTriggerEntity> ImportFromExcel(string filePath)
        {
            return ExcelHelper.ImportFromExcel(filePath, BuildColumns(), () => new PlcTriggerEntity());
        }

        // ---------- 转换辅助 ----------

        private static string ToText(object? v) => v?.ToString()?.Trim() ?? string.Empty;

        private static string? ToNullableText(object? v)
        {
            var text = v?.ToString()?.Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private static string ToEnumText(object? v, string fallback)
        {
            var text = ToText(v).ToLowerInvariant();
            return text == "alarm" || text == "event" || text == "on" || text == "change"
                ? text
                : fallback;
        }

        private static long ToLong(object? v)
        {
            if (v == null) return 0;
            if (v is double d) return (long)d;
            return long.TryParse(v.ToString()?.Trim(), out var r) ? r : 0;
        }

        private static string ToStatus(object? v)
        {
            var text = ToText(v);
            return text == "0" ? "0" : "1";
        }

        private static int ToCategory(object? v)
        {
            if (v is double d) return (int)d;
            if (int.TryParse(v?.ToString()?.Trim(), out var r))
                return r is >= 0 and <= 8 ? r : 0;
            return 0;
        }
    }
}
