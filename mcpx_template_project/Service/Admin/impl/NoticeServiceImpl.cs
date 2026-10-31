using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Helper;

namespace mcpx_template_project.Service.Admin.impl
{
    public class NoticeServiceImpl : INoticeService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;
        private readonly IUserSessionService _session;

        public NoticeServiceImpl(IDbContextFactory<EFDbContext> contextFactory, IUserSessionService session)
        {
            _contextFactory = contextFactory;
            _session = session;
        }

        /// <summary>当前登录用户 ID（未登录返回 0，此时无「已读」概念，全部视为未读）</summary>
        private long CurrentUserId => _session.CurrentUser?.UserId ?? 0;

        public async Task<PagedResult<SysNoticeVO>> QueryNoticesPagedAsync(SysNoticeQuery query, PagingParameters paging)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var queryable = db.SysNotices.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.NoticeTitle))
            {
                queryable = queryable.Where(n =>
                    EF.Functions.Like(n.NoticeTitle, $"%{query.NoticeTitle}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.NoticeType))
            {
                queryable = queryable.Where(n => n.NoticeType == query.NoticeType);
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                queryable = queryable.Where(n => n.Status == query.Status);
            }

            var pagedEntities = await queryable
                .OrderByDescending(n => n.CreateTime)
                .ToPagedResultAsync(paging);

            var userId = CurrentUserId;
            var noticeIds = pagedEntities.Items.Select(n => n.NoticeId).ToList();
            var readIds = await GetReadNoticeIdsAsync(db, userId, noticeIds);

            var vos = pagedEntities.Items.Select(n => MapToVO(n, readIds.Contains(n.NoticeId))).ToList();

            return new PagedResult<SysNoticeVO>
            {
                Items = vos,
                TotalCount = pagedEntities.TotalCount,
                PageIndex = pagedEntities.PageIndex,
                PageSize = pagedEntities.PageSize
            };
        }

        public async Task<SysNoticeVO?> GetNoticeByIdAsync(long noticeId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SysNotices.AsNoTracking()
                .FirstOrDefaultAsync(n => n.NoticeId == noticeId);

            if (entity == null) return null;

            var userId = CurrentUserId;
            var isRead = await db.SysNoticeReads.AsNoTracking()
                .AnyAsync(r => r.NoticeId == noticeId && r.UserId == userId);

            return MapToVO(entity, isRead);
        }

        public async Task<SysNoticeVO> InsertNoticeAsync(SysNotice notice)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            notice.CreateTime = DateTime.Now;
            notice.CreateBy = AppHelper.GetCurrentUserName();
            notice.Source ??= "人工";
            db.SysNotices.Add(notice);
            await db.SaveChangesAsync();

            return MapToVO(notice, false);
        }

        public async Task<SysNoticeVO> UpdateNoticeAsync(SysNotice notice)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SysNotices.FindAsync(notice.NoticeId);
            if (entity == null)
            {
                throw new InvalidOperationException($"通知不存在（ID: {notice.NoticeId}）");
            }

            entity.NoticeTitle = notice.NoticeTitle;
            entity.NoticeType = notice.NoticeType;
            entity.NoticeContent = notice.NoticeContent;
            entity.Status = notice.Status;
            entity.Remark = notice.Remark;
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;

            await db.SaveChangesAsync();

            return MapToVO(entity, false);
        }

        public async Task DeleteNoticeAsync(long noticeId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SysNotices.FindAsync(noticeId);
            if (entity == null)
            {
                throw new InvalidOperationException($"通知不存在（ID: {noticeId}）");
            }

            // 清理该通知的已读记录
            var reads = await db.SysNoticeReads
                .Where(r => r.NoticeId == noticeId)
                .ToListAsync();
            db.SysNoticeReads.RemoveRange(reads);

            db.SysNotices.Remove(entity);
            await db.SaveChangesAsync();
        }

        public async Task SwitchStatusAsync(long noticeId, string status)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            var entity = await db.SysNotices.FindAsync(noticeId);
            if (entity != null)
            {
                entity.Status = status;
                entity.UpdateBy = AppHelper.GetCurrentUserName();
                entity.UpdateTime = DateTime.Now;
                await db.SaveChangesAsync();
            }
        }

        public async Task<int> QueryUnreadCountAsync()
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            var userId = CurrentUserId;
            if (userId == 0) return 0;

            // 正常状态的通知总数 - 当前用户已读数量 = 未读数量
            var total = await db.SysNotices.AsNoTracking()
                .CountAsync(n => n.Status == "0");

            var read = await db.SysNoticeReads.AsNoTracking()
                .CountAsync(r => r.UserId == userId);

            return Math.Max(0, total - read);
        }

        public async Task MarkReadAsync(long noticeId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            var userId = CurrentUserId;
            if (userId == 0) return;

            var exists = await db.SysNoticeReads
                .AnyAsync(r => r.NoticeId == noticeId && r.UserId == userId);
            if (exists) return;

            db.SysNoticeReads.Add(new SysNoticeRead
            {
                NoticeId = noticeId,
                UserId = userId,
                ReadTime = DateTime.Now
            });
            await db.SaveChangesAsync();
        }

        public async Task MarkAllReadAsync()
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            var userId = CurrentUserId;
            if (userId == 0) return;

            var noticeIds = await db.SysNotices.AsNoTracking()
                .Where(n => n.Status == "0")
                .Select(n => n.NoticeId)
                .ToListAsync();

            var existing = await db.SysNoticeReads.AsNoTracking()
                .Where(r => r.UserId == userId)
                .Select(r => r.NoticeId)
                .ToListAsync();

            var toAdd = noticeIds.Except(existing)
                .Select(id => new SysNoticeRead
                {
                    NoticeId = id,
                    UserId = userId,
                    ReadTime = DateTime.Now
                })
                .ToList();

            if (toAdd.Count > 0)
            {
                db.SysNoticeReads.AddRange(toAdd);
                await db.SaveChangesAsync();
            }
        }

        public async Task<long> InsertTerminalNoticeAsync(string title, string content)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var notice = new SysNotice
            {
                NoticeTitle = string.IsNullOrWhiteSpace(title) ? "终端消息" : title,
                NoticeType = "0", // 通知
                NoticeContent = content,
                Source = "S10F3",
                Status = "0",
                CreateTime = DateTime.Now,
                CreateBy = "HOST"
            };

            db.SysNotices.Add(notice);
            await db.SaveChangesAsync();
            return notice.NoticeId;
        }

        /// <summary>查询一批通知中当前用户已读的 ID 集合</summary>
        private static async Task<HashSet<long>> GetReadNoticeIdsAsync(EFDbContext db, long userId, List<long> noticeIds)
        {
            if (userId == 0 || noticeIds.Count == 0) return new HashSet<long>();

            var ids = await db.SysNoticeReads.AsNoTracking()
                .Where(r => r.UserId == userId && noticeIds.Contains(r.NoticeId))
                .Select(r => r.NoticeId)
                .ToListAsync();

            return ids.ToHashSet();
        }

        private static SysNoticeVO MapToVO(SysNotice n, bool isRead)
        {
            return new SysNoticeVO
            {
                NoticeId = n.NoticeId,
                NoticeTitle = n.NoticeTitle,
                NoticeType = n.NoticeType,
                NoticeContent = n.NoticeContent,
                Source = n.Source,
                Status = n.Status,
                CreateBy = n.CreateBy,
                CreateTime = n.CreateTime,
                UpdateBy = n.UpdateBy,
                UpdateTime = n.UpdateTime,
                Remark = n.Remark,
                IsRead = isRead
            };
        }
    }
}
