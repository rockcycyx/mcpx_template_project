using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Helper;
using SecsSxFyEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsSxFy;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// SECS/GEM SxFy 权限表业务服务实现。
    /// 唯一性约束：同一实例下 (stream, function) 不重复。
    /// </summary>
    public class SecsSxFyServiceImpl : ISecsSxFyService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public SecsSxFyServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<SecsSxFyEntity>> QueryAllAsync(long instanceId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.SecsSxFys
                .AsNoTracking()
                .Where(x => x.InstanceId == instanceId)
                .OrderBy(x => x.Stream).ThenBy(x => x.Function)
                .ToListAsync();
        }

        public async Task<PagedResult<SecsSxFyEntity>> QueryPagedAsync(long instanceId, PagingParameters paging, SecsSxFyQuery? query = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            IQueryable<SecsSxFyEntity> q = db.SecsSxFys.AsNoTracking().Where(x => x.InstanceId == instanceId);

            // 三个 bool 筛选可直接下推数据库
            if (query != null)
            {
                if (query.Offline.HasValue)
                    q = q.Where(x => x.AllowOffline == query.Offline.Value);
                if (query.OnlineLocal.HasValue)
                    q = q.Where(x => x.AllowOnlineLocal == query.OnlineLocal.Value);
                if (query.OnlineRemote.HasValue)
                    q = q.Where(x => x.AllowOnlineRemote == query.OnlineRemote.Value);
            }

            // SxF 文本筛选：数据量小，取全量后在内存中匹配（避免 EF 字符串拼接翻译问题）
            var all = await q
                .OrderBy(x => x.Stream).ThenBy(x => x.Function)
                .ToListAsync();

            if (query != null && !string.IsNullOrWhiteSpace(query.SxF))
            {
                string sxF = query.SxF.Trim().ToUpperInvariant();
                all = all.Where(x => x.SxF.ToUpperInvariant().Contains(sxF)).ToList();
            }

            var total = all.Count;
            var pageIndex = Math.Max(1, paging.PageIndex);
            var pageSize = paging.PageSize <= 0 ? 10 : paging.PageSize;

            var items = all
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<SecsSxFyEntity>
            {
                Items = items,
                TotalCount = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<SecsSxFyEntity?> GetByIdAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.SecsSxFys.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<SecsSxFyEntity?> GetByStreamFunctionAsync(long instanceId, byte stream, byte function)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.SecsSxFys
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InstanceId == instanceId && x.Stream == stream && x.Function == function);
        }

        public async Task<SecsSxFyEntity> InsertAsync(SecsSxFyEntity entity)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            await EnsureUniqueAsync(db, entity, null);
            Normalize(entity);

            entity.Id = 0;
            entity.CreateTime = DateTime.Now;
            entity.CreateBy = AppHelper.GetCurrentUserName();

            db.SecsSxFys.Add(entity);
            await db.SaveChangesAsync();
            return entity;
        }

        public async Task<SecsSxFyEntity> UpdateAsync(SecsSxFyEntity entity)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var target = await db.SecsSxFys.FindAsync(entity.Id);
            if (target == null)
                throw new InvalidOperationException($"SxFy 权限条目不存在（ID: {entity.Id}）");

            await EnsureUniqueAsync(db, entity, entity.Id);
            Normalize(entity);

            target.InstanceId = entity.InstanceId;
            target.Stream = entity.Stream;
            target.Function = entity.Function;
            target.AllowOffline = entity.AllowOffline;
            target.AllowOnlineLocal = entity.AllowOnlineLocal;
            target.AllowOnlineRemote = entity.AllowOnlineRemote;
            target.Status = entity.Status;
            target.Name = entity.Name;
            target.UpdateBy = AppHelper.GetCurrentUserName();
            target.UpdateTime = DateTime.Now;

            await db.SaveChangesAsync();
            return target;
        }

        public async Task DeleteAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var target = await db.SecsSxFys.FindAsync(id);
            if (target == null)
                throw new InvalidOperationException($"SxFy 权限条目不存在（ID: {id}）");

            db.SecsSxFys.Remove(target);
            await db.SaveChangesAsync();
        }

        private static void Normalize(SecsSxFyEntity entity)
        {
            entity.Name = entity.Name?.Trim() ?? string.Empty;
            entity.Status = entity.Status == "1" ? "1" : "0";
        }

        private static async Task EnsureUniqueAsync(EFDbContext db, SecsSxFyEntity entity, long? excludeId)
        {
            bool conflict = await db.SecsSxFys.AnyAsync(x =>
                x.InstanceId == entity.InstanceId &&
                x.Stream == entity.Stream &&
                x.Function == entity.Function &&
                (excludeId == null || x.Id != excludeId.Value));

            if (conflict)
                throw new InvalidOperationException(
                    $"S{entity.Stream}F{entity.Function} 在当前实例下已配置，请勿重复添加");
        }
    }
}
