using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Helper;

namespace mcpx_template_project.Service.Admin.impl
{
    public class DeptServiceImpl : IDeptService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public DeptServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<SysDeptVO>> QueryDeptListAsync(SysDeptQuery query)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var queryable = db.SysDepts.AsNoTracking()
                .Where(d => d.DelFlag == "0");

            if (query.DeptId.HasValue && query.DeptId.Value != 0)
            {
                queryable = queryable.Where(d => d.DeptId == query.DeptId.Value);
            }

            if (query.ParentId.HasValue && query.ParentId.Value != 0)
            {
                queryable = queryable.Where(d => d.ParentId == query.ParentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.DeptName))
            {
                queryable = queryable.Where(d =>
                    EF.Functions.Like(d.DeptName, $"%{query.DeptName}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                queryable = queryable.Where(d => d.Status == query.Status);
            }

            var entities = await queryable
                .OrderBy(d => d.ParentId)
                .ThenBy(d => d.OrderNum)
                .ToListAsync();

            return entities.Select(MapToVO).ToList();
        }

        public async Task<List<SysDeptVO>> QueryDeptTreeAsync(SysDeptQuery query)
        {
            var flatList = await QueryDeptListAsync(query);
            return BuildTree(flatList);
        }

        public async Task<SysDept?> GetDeptByIdAsync(long deptId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            return await db.SysDepts.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DeptId == deptId);
        }

        public async Task<ApiResult<SysDept>> InsertDeptAsync(SysDept dept)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            if (string.IsNullOrWhiteSpace(dept.DeptName))
            {
                return new ApiResult<SysDept> { Code = 400, Msg = "部门名称不能为空" };
            }

            dept.CreateTime = DateTime.Now;
            dept.CreateBy = AppHelper.GetCurrentUserName();
            dept.Status ??= "0";
            dept.DelFlag = "0";

            db.SysDepts.Add(dept);
            await db.SaveChangesAsync();

            return new ApiResult<SysDept> { Code = 200, Msg = "新增成功", Data = dept };
        }

        public async Task<ApiResult<SysDept>> UpdateDeptAsync(SysDept dept)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var exists = await db.SysDepts
                .FirstOrDefaultAsync(d => d.DeptId == dept.DeptId);

            if (exists == null)
            {
                return new ApiResult<SysDept> { Code = 400, Msg = "部门不存在" };
            }

            if (string.IsNullOrWhiteSpace(dept.DeptName))
            {
                return new ApiResult<SysDept> { Code = 400, Msg = "部门名称不能为空" };
            }

            exists.ParentId = dept.ParentId;
            exists.DeptName = dept.DeptName;
            exists.OrderNum = dept.OrderNum;
            exists.Leader = dept.Leader;
            exists.Phone = dept.Phone;
            exists.Email = dept.Email;
            exists.Status = dept.Status;
            exists.UpdateBy = AppHelper.GetCurrentUserName();
            exists.UpdateTime = DateTime.Now;

            db.SysDepts.Update(exists);
            await db.SaveChangesAsync();

            return new ApiResult<SysDept> { Code = 200, Msg = "修改成功", Data = exists };
        }

        public async Task<ApiResult<int>> DeleteDeptAsync(long deptId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var dept = await db.SysDepts
                .FirstOrDefaultAsync(d => d.DeptId == deptId);

            if (dept == null)
            {
                return new ApiResult<int> { Code = 400, Msg = "部门不存在", Data = 0 };
            }

            // 检查是否有子部门
            var hasChildren = await db.SysDepts.AnyAsync(d => d.ParentId == deptId && d.DelFlag == "0");
            if (hasChildren)
            {
                return new ApiResult<int> { Code = 400, Msg = "存在子部门,不能删除", Data = 0 };
            }

            // 检查是否有用户关联此部门
            var hasUsers = await db.SysUsers.AnyAsync(u => u.DeptId == deptId);
            if (hasUsers)
            {
                return new ApiResult<int> { Code = 400, Msg = "部门已分配给用户,不能删除", Data = 0 };
            }

            // 软删除
            dept.DelFlag = "2";
            db.SysDepts.Update(dept);
            await db.SaveChangesAsync();

            return new ApiResult<int> { Code = 200, Msg = "删除成功", Data = 1 };
        }

        public async Task SwitchStatusAsync(long deptId, string status)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            var entity = await db.SysDepts.FindAsync(deptId);
            if (entity != null)
            {
                entity.Status = status;
                entity.UpdateBy = AppHelper.GetCurrentUserName();
                entity.UpdateTime = DateTime.Now;
                await db.SaveChangesAsync();
            }
        }

        private static List<SysDeptVO> BuildTree(List<SysDeptVO> flatList)
        {
            var lookup = new Dictionary<long, SysDeptVO>();
            var roots = new List<SysDeptVO>();

            foreach (var item in flatList)
            {
                lookup[item.DeptId] = item;
            }

            foreach (var item in flatList)
            {
                if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parent))
                {
                    parent.Children.Add(item);
                }
                else
                {
                    roots.Add(item);
                }
            }

            return roots;
        }

        private static SysDeptVO MapToVO(SysDept d)
        {
            return new SysDeptVO
            {
                DeptId = d.DeptId,
                ParentId = d.ParentId,
                Ancestors = d.Ancestors,
                DeptName = d.DeptName,
                OrderNum = d.OrderNum,
                Leader = d.Leader,
                Phone = d.Phone,
                Email = d.Email,
                Status = d.Status,
                DelFlag = d.DelFlag,
                CreateBy = d.CreateBy,
                CreateTime = d.CreateTime,
                UpdateBy = d.UpdateBy,
                UpdateTime = d.UpdateTime
            };
        }
    }
}