using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Helper;

namespace mcpx_template_project.Service.Admin.impl
{
    public class PostServiceImpl : IPostService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public PostServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<PagedResult<SysPostVO>> QueryPostsPagedAsync(SysPostQuery query, PagingParameters paging)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var queryable = db.SysPosts.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.PostCode))
            {
                queryable = queryable.Where(p =>
                    EF.Functions.Like(p.PostCode, $"%{query.PostCode}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.PostName))
            {
                queryable = queryable.Where(p =>
                    EF.Functions.Like(p.PostName, $"%{query.PostName}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                queryable = queryable.Where(p => p.Status == query.Status);
            }

            var pagedEntities = await queryable
                .OrderBy(p => p.PostSort)
                .ThenByDescending(p => p.CreateTime)
                .ToPagedResultAsync(paging);

            var posts = pagedEntities.Items.Select(MapToVO).ToList();

            return new PagedResult<SysPostVO>
            {
                Items = posts,
                TotalCount = pagedEntities.TotalCount,
                PageIndex = pagedEntities.PageIndex,
                PageSize = pagedEntities.PageSize
            };
        }

        public async Task<List<SysPostVO>> QueryAllPostsAsync()
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entities = await db.SysPosts.AsNoTracking()
                .OrderBy(p => p.PostSort)
                .ToListAsync();

            return entities.Select(MapToVO).ToList();
        }

        public async Task<List<long>> QueryPostIdsByUserIdAsync(long userId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            return await db.SysUserPosts.AsNoTracking()
                .Where(up => up.UserId == userId)
                .Select(up => up.PostId)
                .ToListAsync();
        }

        public async Task<SysPostVO?> GetPostByIdAsync(long postId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SysPosts.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PostId == postId);

            return entity == null ? null : MapToVO(entity);
        }

        public async Task<SysPostVO> InsertPostAsync(SysPost post)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            // 检查岗位编码是否唯一
            if (await db.SysPosts.AnyAsync(p => p.PostCode == post.PostCode))
            {
                throw new InvalidOperationException($"岗位编码 '{post.PostCode}' 已存在");
            }

            post.CreateTime = DateTime.Now;
            post.CreateBy = AppHelper.GetCurrentUserName();
            db.SysPosts.Add(post);
            await db.SaveChangesAsync();

            return MapToVO(post);
        }

        public async Task<SysPostVO> UpdatePostAsync(SysPost post)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SysPosts.FindAsync(post.PostId);
            if (entity == null)
            {
                throw new InvalidOperationException($"岗位不存在（ID: {post.PostId}）");
            }

            // 检查岗位编码是否唯一（排除自身）
            if (await db.SysPosts.AnyAsync(p => p.PostCode == post.PostCode && p.PostId != post.PostId))
            {
                throw new InvalidOperationException($"岗位编码 '{post.PostCode}' 已存在");
            }

            entity.PostCode = post.PostCode;
            entity.PostName = post.PostName;
            entity.PostSort = post.PostSort;
            entity.Status = post.Status;
            entity.Remark = post.Remark;
            entity.UpdateBy = AppHelper.GetCurrentUserName();
            entity.UpdateTime = DateTime.Now;

            await db.SaveChangesAsync();

            return MapToVO(entity);
        }

        public async Task DeletePostAsync(long postId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entity = await db.SysPosts.FindAsync(postId);
            if (entity == null)
            {
                throw new InvalidOperationException($"岗位不存在（ID: {postId}）");
            }

            // 检查是否有用户关联此岗位
            bool hasUsers = await db.SysUserPosts.AnyAsync(up => up.PostId == postId);
            if (hasUsers)
            {
                throw new InvalidOperationException("该岗位下存在用户，无法删除");
            }

            db.SysPosts.Remove(entity);
            await db.SaveChangesAsync();
        }

        public async Task SwitchStatusAsync(long postId, string status)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            var entity = await db.SysPosts.FindAsync(postId);
            if (entity != null)
            {
                entity.Status = status;
                entity.UpdateBy = AppHelper.GetCurrentUserName();
                entity.UpdateTime = DateTime.Now;
                await db.SaveChangesAsync();
            }
        }

        private static SysPostVO MapToVO(SysPost p)
        {
            return new SysPostVO
            {
                PostId = p.PostId,
                PostCode = p.PostCode,
                PostName = p.PostName,
                PostSort = p.PostSort,
                Status = p.Status,
                CreateBy = p.CreateBy,
                CreateTime = p.CreateTime,
                UpdateBy = p.UpdateBy,
                UpdateTime = p.UpdateTime,
                Remark = p.Remark
            };
        }
    }
}