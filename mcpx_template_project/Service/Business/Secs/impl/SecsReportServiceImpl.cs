using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Helper;
using SecsReportEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsReport;
using SecsReportVidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsReportVid;
using SecsVidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsVid;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// SECS/GEM Report 业务服务实现。
    /// 唯一性约束：report_id 不重复。
    /// VID 成员通过 secs_report_vid 子表维护，sort 按传入 vidIds 的顺序从 1 递增编号。
    /// </summary>
    public class SecsReportServiceImpl : ISecsReportService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public SecsReportServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<SecsReportEntity>> QueryAllAsync(long instanceId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            return await BuildQuery(db, instanceId).ToListAsync();
        }

        public async Task<PagedResult<SecsReportEntity>> QueryPagedAsync(long instanceId, PagingParameters paging)
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

            return new PagedResult<SecsReportEntity>
            {
                Items = items,
                TotalCount = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        private static IQueryable<SecsReportEntity> BuildQuery(EFDbContext db, long instanceId)
        {
            return db.SecsReports
                .AsNoTracking()
                .Where(r => r.InstanceId == instanceId)
                .Include(r => r.Vids.OrderBy(v => v.Sort))
                    .ThenInclude(v => v.Vid)
                .OrderBy(r => r.ReportId);
        }

        public async Task<SecsReportEntity?> GetByIdAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            return await db.SecsReports
                .AsNoTracking()
                .Include(r => r.Vids.OrderBy(v => v.Sort))
                    .ThenInclude(v => v.Vid)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<SecsReportEntity> InsertAsync(SecsReportEntity report, IReadOnlyList<long> vidIds)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            await EnsureUniqueAsync(db, report, null);
            Normalize(report);

            report.Id = 0;
            report.CreateTime = DateTime.Now;
            report.CreateBy = AppHelper.GetCurrentUserName();
            report.Vids = BuildVids(vidIds);

            db.SecsReports.Add(report);
            await db.SaveChangesAsync();
            return report;
        }

        public async Task<SecsReportEntity> UpdateAsync(SecsReportEntity report, IReadOnlyList<long> vidIds)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SecsReports
                .Include(r => r.Vids)
                .FirstOrDefaultAsync(r => r.Id == report.Id);

            if (entity == null)
                throw new InvalidOperationException($"Report 不存在（ID: {report.Id}）");

            await EnsureUniqueAsync(db, report, report.Id);
            Normalize(report);

            entity.InstanceId = report.InstanceId;
            entity.ReportId = report.ReportId;
            entity.Name = report.Name;
            entity.Status = report.Status;
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;

            // 覆盖成员 VID：先清空旧明细，再写入新明细
            db.SecsReportVids.RemoveRange(entity.Vids);
            entity.Vids = BuildVids(vidIds);

            await db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateStatusAsync(long id, string status)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SecsReports.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"Report 不存在（ID: {id}）");

            entity.Status = status == "1" ? "1" : "0";
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SecsReports
                .Include(r => r.Vids)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity == null)
                throw new InvalidOperationException($"Report 不存在（ID: {id}）");

            db.SecsReports.Remove(entity);
            await db.SaveChangesAsync();
        }

        public async Task<List<SecsVidEntity>> QueryVidsAsync(long instanceId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            return await db.SecsVids
                .AsNoTracking()
                .Where(v => v.InstanceId == instanceId && v.Status == "0")
                .OrderBy(v => v.VidType)
                .ThenBy(v => v.VidId)
                .ToListAsync();
        }

        /// <summary>按传入顺序构建成员明细，sort 从 1 递增。</summary>
        private static List<SecsReportVidEntity> BuildVids(IReadOnlyList<long> vidIds)
        {
            var list = new List<SecsReportVidEntity>(vidIds.Count);
            for (int i = 0; i < vidIds.Count; i++)
            {
                list.Add(new SecsReportVidEntity
                {
                    VidId = vidIds[i],
                    Sort = i + 1,
                });
            }
            return list;
        }

        /// <summary>规范化字段：name 去首尾空格、status 归一（0=启用，1=停用）。</summary>
        private static void Normalize(SecsReportEntity report)
        {
            report.Name = report.Name?.Trim() ?? string.Empty;
            report.Status = report.Status == "1" ? "1" : "0";
        }

        /// <summary>校验同实例内 report_id 不重复。</summary>
        private static async Task EnsureUniqueAsync(EFDbContext db, SecsReportEntity report, long? excludeId)
        {
            bool conflict = await db.SecsReports.AnyAsync(r =>
                r.InstanceId == report.InstanceId &&
                r.ReportId == report.ReportId &&
                (excludeId == null || r.Id != excludeId.Value));

            if (conflict)
                throw new InvalidOperationException($"Report 编号 {report.ReportId} 已存在");
        }
    }
}
