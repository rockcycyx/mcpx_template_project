using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Helper;
using SecsVidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsVid;
using SecsVidType = mcpx_template_project.Domain.Entities.Business.Secs.SecsVidType;

namespace mcpx_template_project.Service.Business.Secs.impl
{
    /// <summary>
    /// SECS/GEM VID 业务服务实现。
    /// 唯一性约束：同 vid_type 内 vid_id 不重复。
    /// </summary>
    public class SecsVidServiceImpl : ISecsVidService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public SecsVidServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<SecsVidEntity>> QueryAllAsync(long instanceId, SecsVidType? vidType = null, string? keyword = null)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await BuildQuery(db, instanceId, vidType, keyword).ToListAsync();
        }

        public async Task<PagedResult<SecsVidEntity>> QueryPagedAsync(
            long instanceId,
            SecsVidType? vidType,
            string? keyword,
            string? deviceCode,
            string? plcAddress,
            string? status,
            PagingParameters paging)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var queryable = BuildQuery(db, instanceId, vidType, keyword, deviceCode, plcAddress, status);

            var total = await queryable.CountAsync();
            var pageIndex = Math.Max(1, paging.PageIndex);
            var pageSize = paging.PageSize <= 0 ? 10 : paging.PageSize;

            var items = await queryable
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<SecsVidEntity>
            {
                Items = items,
                TotalCount = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        private static IQueryable<SecsVidEntity> BuildQuery(
            EFDbContext db,
            long instanceId,
            SecsVidType? vidType,
            string? keyword,
            string? deviceCode = null,
            string? plcAddress = null,
            string? status = null)
        {
            IQueryable<SecsVidEntity> queryable = db.SecsVids.AsNoTracking()
                .Where(v => v.InstanceId == instanceId);

            if (vidType.HasValue)
                queryable = queryable.Where(v => v.VidType == vidType.Value);

            if (!string.IsNullOrWhiteSpace(keyword))
                queryable = queryable.Where(v => EF.Functions.Like(v.Name, $"%{keyword}%"));

            if (!string.IsNullOrWhiteSpace(deviceCode))
                queryable = queryable.Where(v => v.DeviceCode != null && EF.Functions.Like(v.DeviceCode, $"%{deviceCode}%"));

            if (!string.IsNullOrWhiteSpace(plcAddress))
                queryable = queryable.Where(v => v.PlcAddress != null && EF.Functions.Like(v.PlcAddress, $"%{plcAddress}%"));

            if (!string.IsNullOrWhiteSpace(status))
                queryable = queryable.Where(v => v.Status == status);

            return queryable
                .OrderBy(v => v.VidType)
                .ThenBy(v => v.VidId);
        }

        public async Task<SecsVidEntity?> GetByIdAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.SecsVids.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<SecsVidEntity> InsertAsync(SecsVidEntity vid)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            await EnsureUniqueAsync(db, vid, null);
            Normalize(vid);

            vid.Id = 0;
            vid.CreateTime = DateTime.Now;
            vid.CreateBy = AppHelper.GetCurrentUserName();

            db.SecsVids.Add(vid);
            await db.SaveChangesAsync();
            return vid;
        }

        public async Task<SecsVidEntity> UpdateAsync(SecsVidEntity vid)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SecsVids.FindAsync(vid.Id);
            if (entity == null)
                throw new InvalidOperationException($"VID 不存在（ID: {vid.Id}）");

            await EnsureUniqueAsync(db, vid, vid.Id);
            Normalize(vid);

            entity.InstanceId = vid.InstanceId;
            entity.VidType = vid.VidType;
            entity.VidId = vid.VidId;
            entity.Name = vid.Name;
            entity.DataType = vid.DataType;
            entity.Source = vid.Source;
            entity.PlcAddress = vid.PlcAddress;
            entity.DeviceCode = vid.DeviceCode;
            entity.DefaultValue = vid.DefaultValue;
            entity.Status = vid.Status;
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;

            await db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateStatusAsync(long id, string status)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SecsVids.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"VID 不存在（ID: {id}）");

            entity.Status = status == "1" ? "1" : "0";
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SecsVids.FindAsync(id);
            if (entity == null)
                throw new InvalidOperationException($"VID 不存在（ID: {id}）");

            db.SecsVids.Remove(entity);
            await db.SaveChangesAsync();
        }

        /// <summary>规范化字段：source 小写、status 归一（0=启用，1=停用）。</summary>
        private static void Normalize(SecsVidEntity vid)
        {
            vid.Source = vid.Source?.Trim().ToLowerInvariant() ?? "db";
            vid.Status = vid.Status == "1" ? "1" : "0";

            // 非 plc 来源时清空 PLC 地址与设备代号
            if (vid.Source != "plc")
            {
                vid.PlcAddress = null;
                vid.DeviceCode = null;
            }
            // plc 来源时清空固定值（值取自 PLC 缓存，固定值无意义）
            else
            {
                vid.DefaultValue = null;
            }
        }

        /// <summary>校验同实例 + 同 vid_type 内 vid_id 不重复。</summary>
        private static async Task EnsureUniqueAsync(EFDbContext db, SecsVidEntity vid, long? excludeId)
        {
            bool conflict = await db.SecsVids.AnyAsync(v =>
                v.InstanceId == vid.InstanceId &&
                v.VidType == vid.VidType &&
                v.VidId == vid.VidId &&
                (excludeId == null || v.Id != excludeId.Value));

            if (conflict)
                throw new InvalidOperationException(
                    $"{vid.VidType} 编号 {vid.VidId} 已存在");
        }
    }
}
