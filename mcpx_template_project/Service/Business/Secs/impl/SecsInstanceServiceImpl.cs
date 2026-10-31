using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Helper;
using SecsInstanceEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsInstance;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// SECS/GEM 实例业务服务实现。
    /// 唯一性约束：同一 ip_address + port 不重复（避免监听冲突）。
    /// </summary>
    public class SecsInstanceServiceImpl : ISecsInstanceService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public SecsInstanceServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<SecsInstanceEntity>> QueryAllAsync(string? keyword = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await BuildQuery(db, keyword).ToListAsync();
        }

        public async Task<PagedResult<SecsInstanceEntity>> QueryPagedAsync(PagingParameters paging, string? keyword = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var queryable = BuildQuery(db, keyword);
            var total = await queryable.CountAsync();
            var pageIndex = Math.Max(1, paging.PageIndex);
            var pageSize = paging.PageSize <= 0 ? 10 : paging.PageSize;

            var items = await queryable
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<SecsInstanceEntity>
            {
                Items = items,
                TotalCount = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<List<SecsInstanceEntity>> QueryEnabledAsync()
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.SecsInstances
                .AsNoTracking()
                .Where(i => i.Status == "0")
                .OrderBy(i => i.Id)
                .ToListAsync();
        }

        private static IQueryable<SecsInstanceEntity> BuildQuery(EFDbContext db, string? keyword)
        {
            IQueryable<SecsInstanceEntity> queryable = db.SecsInstances.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
                queryable = queryable.Where(i => EF.Functions.Like(i.Name, $"%{keyword}%"));

            return queryable.OrderBy(i => i.Id);
        }

        public async Task<SecsInstanceEntity?> GetByIdAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.SecsInstances.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<SecsInstanceEntity> InsertAsync(SecsInstanceEntity instance)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            await EnsureUniqueAsync(db, instance, null);
            Normalize(instance);

            instance.Id = 0;
            instance.CreateTime = DateTime.Now;
            instance.CreateBy = AppHelper.GetCurrentUserName();

            db.SecsInstances.Add(instance);
            await db.SaveChangesAsync();
            return instance;
        }

        public async Task<SecsInstanceEntity> UpdateAsync(SecsInstanceEntity instance)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SecsInstances.FindAsync(instance.Id);
            if (entity == null)
                throw new InvalidOperationException($"SECS 实例不存在（ID: {instance.Id}）");

            await EnsureUniqueAsync(db, instance, instance.Id);
            Normalize(instance);

            entity.Name = instance.Name;
            entity.IpAddress = instance.IpAddress;
            entity.Port = instance.Port;
            entity.DeviceId = instance.DeviceId;
            entity.ConnectMode = instance.ConnectMode;
            entity.T3 = instance.T3;
            entity.T5 = instance.T5;
            entity.T6 = instance.T6;
            entity.T7 = instance.T7;
            entity.T8 = instance.T8;
            entity.CommSettingId = instance.CommSettingId;
            entity.Status = instance.Status;
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;

            await db.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SecsInstances.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"SECS 实例不存在（ID: {id}）");

            db.SecsInstances.Remove(entity);
            await db.SaveChangesAsync();
        }

        /// <summary>规范化字段：name 去首尾空格、ip 去空格、connect_mode 归一、status 归一。</summary>
        private static void Normalize(SecsInstanceEntity instance)
        {
            instance.Name = instance.Name?.Trim() ?? string.Empty;
            instance.IpAddress = instance.IpAddress?.Trim() ?? "0.0.0.0";
            instance.ConnectMode = string.Equals(instance.ConnectMode, "auto", StringComparison.OrdinalIgnoreCase)
                ? "auto"
                : "manual";
            instance.Status = instance.Status == "1" ? "1" : "0";
        }

        /// <summary>校验同一 ip_address + port 不重复（监听冲突）。</summary>
        private static async Task EnsureUniqueAsync(EFDbContext db, SecsInstanceEntity instance, long? excludeId)
        {
            bool conflict = await db.SecsInstances.AnyAsync(i =>
                i.IpAddress == instance.IpAddress &&
                i.Port == instance.Port &&
                (excludeId == null || i.Id != excludeId.Value));

            if (conflict)
                throw new InvalidOperationException(
                    $"监听地址 {instance.IpAddress}:{instance.Port} 已被其它实例占用");
        }
    }
}
