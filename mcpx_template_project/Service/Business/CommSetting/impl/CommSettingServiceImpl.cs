using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Helper;
using mcpx_template_project.Domain.Entities.Business.CommSetting;
using CommSettingEntity = mcpx_template_project.Domain.Entities.Business.CommSetting.CommSetting;

namespace mcpx_template_project.Service.Business.CommSetting.impl
{
    /// <summary>
    /// 通讯设定业务服务实现
    /// </summary>
    public class CommSettingServiceImpl : ICommSettingService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public CommSettingServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<CommSettingVO>> QueryAllAsync(string? keyword = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            IQueryable<CommSettingEntity> queryable = db.CommSettings.AsNoTracking().Include(s => s.Areas);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                queryable = queryable.Where(s =>
                    EF.Functions.Like(s.Name, $"%{keyword}%") ||
                    EF.Functions.Like(s.Ip, $"%{keyword}%"));
            }

            var entities = await queryable
                .OrderBy(s => s.Id)
                .ToListAsync();

            return entities.Select(MapToVO).ToList();
        }

        public async Task<CommSettingVO?> GetByIdAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.CommSettings.AsNoTracking()
                .Include(s => s.Areas)
                .FirstOrDefaultAsync(s => s.Id == id);

            return entity == null ? null : MapToVO(entity);
        }

        public async Task<CommSettingVO> InsertAsync(CommSettingEntity setting)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            if (await db.CommSettings.AnyAsync(s => s.Name == setting.Name))
                throw new InvalidOperationException($"通讯设定名称 '{setting.Name}' 已存在");

            setting.CreateTime = DateTime.Now;
            setting.CreateBy = AppHelper.GetCurrentUserName();

            db.CommSettings.Add(setting);
            await db.SaveChangesAsync();

            return MapToVO(setting);
        }

        public async Task<CommSettingVO> UpdateAsync(CommSettingEntity setting)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.CommSettings.FindAsync(setting.Id);
            if (entity == null)
                throw new InvalidOperationException($"通讯设定不存在（ID: {setting.Id}）");

            if (await db.CommSettings.AnyAsync(s => s.Name == setting.Name && s.Id != setting.Id))
                throw new InvalidOperationException($"通讯设定名称 '{setting.Name}' 已存在");

            entity.Name = setting.Name;
            entity.Ip = setting.Ip;
            entity.Port = setting.Port;
            entity.ConnectMode = setting.ConnectMode;
            entity.CheckCycleMs = setting.CheckCycleMs;
            entity.TimeoutMs = setting.TimeoutMs;
            entity.RetryCycleS = setting.RetryCycleS;
            entity.Remark = setting.Remark;
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;

            await db.SaveChangesAsync();

            return MapToVO(entity);
        }

        public async Task SaveAreasAsync(long settingId, List<CommSettingArea> areas)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.CommSettings
                .Include(s => s.Areas)
                .FirstOrDefaultAsync(s => s.Id == settingId);
            if (entity == null)
                throw new InvalidOperationException($"通讯设定不存在（ID: {settingId}）");

            // 单身明细：先删后插（保持简单可靠）
            db.CommSettingAreas.RemoveRange(entity.Areas);
            entity.Areas = NormalizeAreas(areas);
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;

            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.CommSettings.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"通讯设定不存在（ID: {id}）");

            // 显式删除单身明细，避免孤儿数据
            var areas = await db.CommSettingAreas
                .Where(a => a.SettingId == id)
                .ToListAsync();
            db.CommSettingAreas.RemoveRange(areas);

            db.CommSettings.Remove(entity);
            await db.SaveChangesAsync();
        }

        public async Task SwitchRunAsync(long id, string status)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.CommSettings.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"通讯设定不存在（ID: {id}）");

            entity.Status = status;
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;
            await db.SaveChangesAsync();
        }

        private static List<CommSettingArea> NormalizeAreas(List<CommSettingArea> areas)
        {
            var list = areas ?? new List<CommSettingArea>();
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Id = 0;        // 重新插入，避免主键冲突
                list[i].SettingId = 0;  // 由导航属性自动关联外键
                list[i].Sort = i + 1;
            }
            return list;
        }

        private static CommSettingVO MapToVO(CommSettingEntity s)
        {
            return new CommSettingVO
            {
                Id = s.Id,
                Name = s.Name,
                Ip = s.Ip,
                Port = s.Port,
                ConnectMode = s.ConnectMode,
                CheckCycleMs = s.CheckCycleMs,
                TimeoutMs = s.TimeoutMs,
                RetryCycleS = s.RetryCycleS,
                Status = s.Status,
                Remark = s.Remark,
                CreateBy = s.CreateBy,
                CreateTime = s.CreateTime,
                UpdateBy = s.UpdateBy,
                UpdateTime = s.UpdateTime,
                Areas = (s.Areas ?? new List<CommSettingArea>())
                    .OrderBy(a => a.Sort)
                    .ThenBy(a => a.Id)
                    .Select(MapAreaToVO)
                    .ToList()
            };
        }

        private static CommSettingAreaVO MapAreaToVO(CommSettingArea a)
        {
            return new CommSettingAreaVO
            {
                Id = a.Id,
                SettingId = a.SettingId,
                AreaType = a.AreaType,
                Name = a.Name,
                Position = a.Position,
                Quantity = a.Quantity,
                Sort = a.Sort
            };
        }
    }
}
