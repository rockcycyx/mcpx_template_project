using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Helper;
using SecsCeidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsCeid;
using SecsCeidReportEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsCeidReport;
using SecsReportEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsReport;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// SECS/GEM 收集事件（CEID/Event）业务服务实现。
    /// 唯一性约束：ceid 不重复。
    /// Report 成员通过 secs_ceid_report 子表维护，sort 按传入 reportIds 的顺序从 1 递增编号。
    /// </summary>
    public class SecsCeidServiceImpl : ISecsCeidService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public SecsCeidServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<SecsCeidEntity>> QueryAllAsync(long instanceId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            return await BuildQuery(db, instanceId).ToListAsync();
        }

        public async Task<PagedResult<SecsCeidEntity>> QueryPagedAsync(long instanceId, PagingParameters paging)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var queryable = BuildQuery(db, instanceId);
            var total = await queryable.CountAsync();
            var pageIndex = Math.Max(1, paging.PageIndex);
            var pageSize = paging.PageSize <= 0 ? 10 : paging.PageSize;

            var items = await queryable
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<SecsCeidEntity>
            {
                Items = items,
                TotalCount = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        private static IQueryable<SecsCeidEntity> BuildQuery(EFDbContext db, long instanceId)
        {
            return db.SecsCeids
                .AsNoTracking()
                .Where(c => c.InstanceId == instanceId)
                .Include(c => c.Reports.OrderBy(r => r.Sort))
                    .ThenInclude(r => r.Report)
                .OrderBy(c => c.Ceid);
        }

        public async Task<SecsCeidEntity?> GetByIdAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            return await db.SecsCeids
                .AsNoTracking()
                .Include(c => c.Reports.OrderBy(r => r.Sort))
                    .ThenInclude(r => r.Report)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<SecsCeidEntity> InsertAsync(SecsCeidEntity ceid, IReadOnlyList<long> reportIds)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            await EnsureUniqueAsync(db, ceid, null);
            Normalize(ceid);

            ceid.Id = 0;
            ceid.CreateTime = DateTime.Now;
            ceid.CreateBy = AppHelper.GetCurrentUserName();
            ceid.Reports = BuildReports(reportIds);

            db.SecsCeids.Add(ceid);
            await db.SaveChangesAsync();
            return ceid;
        }

        public async Task<SecsCeidEntity> UpdateAsync(SecsCeidEntity ceid, IReadOnlyList<long> reportIds)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SecsCeids
                .Include(c => c.Reports)
                .FirstOrDefaultAsync(c => c.Id == ceid.Id);

            if (entity == null)
                throw new InvalidOperationException($"CEID 不存在（ID: {ceid.Id}）");

            await EnsureUniqueAsync(db, ceid, ceid.Id);
            Normalize(ceid);

            entity.InstanceId = ceid.InstanceId;
            entity.Ceid = ceid.Ceid;
            entity.Name = ceid.Name;
            entity.Status = ceid.Status;
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;

            // 覆盖成员 Report：先清空旧明细，再写入新明细
            db.SecsCeidReports.RemoveRange(entity.Reports);
            entity.Reports = BuildReports(reportIds);

            await db.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SecsCeids
                .Include(c => c.Reports)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (entity == null)
                throw new InvalidOperationException($"CEID 不存在（ID: {id}）");

            db.SecsCeids.Remove(entity);
            await db.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(long id, string status)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SecsCeids.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"CEID 不存在（ID: {id}）");

            entity.Status = status == "1" ? "1" : "0";
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;
            await db.SaveChangesAsync();
        }

        public async Task<List<SecsReportEntity>> QueryReportsAsync(long instanceId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            return await db.SecsReports
                .AsNoTracking()
                .Where(r => r.InstanceId == instanceId && r.Status == "0")
                .OrderBy(r => r.ReportId)
                .ToListAsync();
        }

        /// <summary>按传入顺序构建成员明细，sort 从 1 递增。</summary>
        private static List<SecsCeidReportEntity> BuildReports(IReadOnlyList<long> reportIds)
        {
            var list = new List<SecsCeidReportEntity>(reportIds.Count);
            for (int i = 0; i < reportIds.Count; i++)
            {
                list.Add(new SecsCeidReportEntity
                {
                    ReportId = reportIds[i],
                    Sort = i + 1,
                });
            }
            return list;
        }

        /// <summary>规范化字段：name 去首尾空格、status 归一（0=启用，1=停用）。</summary>
        private static void Normalize(SecsCeidEntity ceid)
        {
            ceid.Name = ceid.Name?.Trim() ?? string.Empty;
            ceid.Status = ceid.Status == "1" ? "1" : "0";
        }

        /// <summary>校验同实例内 ceid 不重复。</summary>
        private static async Task EnsureUniqueAsync(EFDbContext db, SecsCeidEntity ceid, long? excludeId)
        {
            bool conflict = await db.SecsCeids.AnyAsync(c =>
                c.InstanceId == ceid.InstanceId &&
                c.Ceid == ceid.Ceid &&
                (excludeId == null || c.Id != excludeId.Value));

            if (conflict)
                throw new InvalidOperationException($"CEID 编号 {ceid.Ceid} 已存在");
        }
    }
}
