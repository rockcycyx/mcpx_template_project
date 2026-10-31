using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using mcpx_template_project.Configation;
using mcpx_template_project.Core.Logging;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Helper;

namespace mcpx_template_project.Service.Admin.impl
{
    public class OperLogService : IOperLogService
    {
        private readonly IDbContextFactory<EFDbContext> _dbContextFactory;
        private readonly ILogService _logService;

        public OperLogService(
            IDbContextFactory<EFDbContext> dbContextFactory,
            ILogService logService)
        {
            _dbContextFactory = dbContextFactory;
            _logService = logService;
        }

        /// <summary>
        /// 异步保存操作日志到数据库
        /// </summary>
        public async Task SaveOperLogAsync(SysOperLog operLog)
        {
            try
            {
                // 填充当前用户信息
                operLog.OperName = AppHelper.GetCurrentUserName();
                // 获取本机IP地址
                operLog.OperIp = AppHelper.GetLocalIpAddress();
                // 设置操作时间
                operLog.OperTime = DateTime.Now;

                // 异步保存到数据库
                using var dbContext = _dbContextFactory.CreateDbContext();
                dbContext.SysOperLogs.Add(operLog);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // 日志记录失败不影响业务流程
                _logService.LogError(ex, $"保存操作日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分页查询操作日志
        /// </summary>
        public async Task<PagedResult<SysOperLogVO>> QueryOperLogsPagedAsync(SysOperLogQuery query, PagingParameters paging)
        {
            using var db = _dbContextFactory.CreateDbContext();

            var queryable = db.SysOperLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Title))
            {
                queryable = queryable.Where(p => EF.Functions.Like(p.Title, $"%{query.Title}%"));
            }

            if (query.BusinessType.HasValue)
            {
                queryable = queryable.Where(p => p.BusinessType == query.BusinessType.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.OperName))
            {
                queryable = queryable.Where(p => EF.Functions.Like(p.OperName, $"%{query.OperName}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.OperIp))
            {
                queryable = queryable.Where(p => EF.Functions.Like(p.OperIp, $"%{query.OperIp}%"));
            }

            if (query.Status.HasValue)
            {
                queryable = queryable.Where(p => p.Status == query.Status.Value);
            }

            if (query.BeginTime.HasValue)
            {
                queryable = queryable.Where(p => p.OperTime >= query.BeginTime.Value);
            }

            if (query.EndTime.HasValue)
            {
                queryable = queryable.Where(p => p.OperTime <= query.EndTime.Value);
            }

            queryable = queryable.OrderByDescending(p => p.OperId);

            var pagedEntities = await queryable.ToPagedResultAsync(paging);

            var logs = pagedEntities.Items.Select(MapToVO).ToList();

            return new PagedResult<SysOperLogVO>
            {
                Items = logs,
                TotalCount = pagedEntities.TotalCount,
                PageIndex = pagedEntities.PageIndex,
                PageSize = pagedEntities.PageSize
            };
        }

        /// <summary>
        /// 根据ID查询操作日志
        /// </summary>
        public async Task<SysOperLogVO?> GetOperLogByIdAsync(long operId)
        {
            using var db = _dbContextFactory.CreateDbContext();

            var entity = await db.SysOperLogs.AsNoTracking()
                .FirstOrDefaultAsync(p => p.OperId == operId);

            return entity == null ? null : MapToVO(entity);
        }

        /// <summary>
        /// 删除操作日志
        /// </summary>
        public async Task DeleteOperLogAsync(long operId)
        {
            using var db = _dbContextFactory.CreateDbContext();

            var entity = await db.SysOperLogs.FindAsync(operId);
            if (entity == null)
            {
                throw new InvalidOperationException($"操作日志不存在（ID: {operId}）");
            }

            db.SysOperLogs.Remove(entity);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// 清空操作日志
        /// </summary>
        public async Task ClearOperLogsAsync()
        {
            using var db = _dbContextFactory.CreateDbContext();

            await db.Database.ExecuteSqlRawAsync("DELETE FROM sys_oper_log");
        }

        private static SysOperLogVO MapToVO(SysOperLog p)
        {
            return new SysOperLogVO
            {
                OperId = p.OperId,
                Title = p.Title,
                BusinessType = p.BusinessType,
                Method = p.Method,
                OperatorType = p.OperatorType,
                OperName = p.OperName,
                DeptName = p.DeptName,
                OperIp = p.OperIp,
                OperLocation = p.OperLocation,
                OperParam = p.OperParam,
                JsonResult = p.JsonResult,
                Status = p.Status,
                ErrorMsg = p.ErrorMsg,
                OperTime = p.OperTime,
                CostTime = p.CostTime
            };
        }
    }
}