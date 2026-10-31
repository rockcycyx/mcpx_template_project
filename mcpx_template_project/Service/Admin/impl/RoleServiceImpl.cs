using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Helper;

namespace mcpx_template_project.Service.Admin.impl
{
    public class RoleServiceImpl : IRoleService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public RoleServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<PagedResult<SysRoleVO>> QueryRolesPagedAsync(SysRoleQuery query, PagingParameters paging)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var queryable = db.SysRoles.AsNoTracking()
                .Where(r => r.DelFlag == "0");

            if (!string.IsNullOrWhiteSpace(query.RoleName))
            {
                queryable = queryable.Where(r =>
                    EF.Functions.Like(r.RoleName, $"%{query.RoleName}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.RoleKey))
            {
                queryable = queryable.Where(r =>
                    EF.Functions.Like(r.RoleKey, $"%{query.RoleKey}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                queryable = queryable.Where(r => r.Status == query.Status);
            }

            if (query.BeginTime.HasValue)
            {
                var beginDate = query.BeginTime.Value.Date;
                queryable = queryable.Where(r =>
                    r.CreateTime.HasValue && r.CreateTime.Value >= beginDate);
            }

            if (query.EndTime.HasValue)
            {
                var endDate = query.EndTime.Value.Date.AddDays(1);
                queryable = queryable.Where(r =>
                    r.CreateTime.HasValue && r.CreateTime.Value < endDate);
            }

            var pagedEntities = await queryable
                .OrderBy(r => r.RoleSort)
                .ToPagedResultAsync(paging);

            var roles = pagedEntities.Items.Select(MapToVO).ToList();

            return new PagedResult<SysRoleVO>
            {
                Items = roles,
                TotalCount = pagedEntities.TotalCount,
                PageIndex = pagedEntities.PageIndex,
                PageSize = pagedEntities.PageSize
            };
        }

        public async Task<List<long>> QueryRoleIdsByUserIdAsync(long userId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            return await db.SysUserRoles.AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();
        }

        public async Task<List<SysRoleVO>> QueryRolesByIdsAsync(List<long> roleIds)
        {
            if (roleIds == null || roleIds.Count == 0)
                return new List<SysRoleVO>();

            using var db = await _contextFactory.CreateDbContextAsync();

            var entities = await db.SysRoles.AsNoTracking()
                .Where(r => roleIds.Contains(r.RoleId) && r.DelFlag == "0")
                .OrderBy(r => r.RoleSort)
                .ToListAsync();

            return entities.Select(MapToVO).ToList();
        }

        public async Task<List<SysRoleVO>> QueryAllActiveRolesAsync()
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entities = await db.SysRoles.AsNoTracking()
                .Where(r => r.DelFlag == "0" && r.Status == "0")
                .OrderBy(r => r.RoleSort)
                .ToListAsync();

            return entities.Select(MapToVO).ToList();
        }

        public async Task<ApiResult<SysRole>> InsertRoleAsync(SysRole role)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            if (string.IsNullOrWhiteSpace(role.RoleName))
            {
                return new ApiResult<SysRole> { Code = 400, Msg = "角色名称不能为空" };
            }

            if (string.IsNullOrWhiteSpace(role.RoleKey))
            {
                return new ApiResult<SysRole> { Code = 400, Msg = "权限字符不能为空" };
            }

            // 校验权限字符唯一
            var exists = await db.SysRoles
                .AnyAsync(r => r.RoleKey == role.RoleKey && r.DelFlag == "0");

            if (exists)
            {
                return new ApiResult<SysRole> { Code = 400, Msg = $"权限字符 '{role.RoleKey}' 已存在" };
            }

            role.CreateTime = DateTime.Now;
            role.CreateBy = AppHelper.GetCurrentUserName();
            role.Status ??= "0";
            role.DelFlag = "0";

            db.SysRoles.Add(role);
            await db.SaveChangesAsync();

            return new ApiResult<SysRole> { Code = 200, Msg = "新增成功", Data = role };
        }

        public async Task<ApiResult<SysRole>> UpdateRoleAsync(SysRole role)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var exists = await db.SysRoles
                .FirstOrDefaultAsync(r => r.RoleId == role.RoleId);

            if (exists == null)
            {
                return new ApiResult<SysRole> { Code = 400, Msg = "角色不存在" };
            }

            if (string.IsNullOrWhiteSpace(role.RoleName))
            {
                return new ApiResult<SysRole> { Code = 400, Msg = "角色名称不能为空" };
            }

            if (string.IsNullOrWhiteSpace(role.RoleKey))
            {
                return new ApiResult<SysRole> { Code = 400, Msg = "权限字符不能为空" };
            }

            // 校验权限字符唯一（排除自身）
            var duplicateKey = await db.SysRoles
                .AnyAsync(r => r.RoleKey == role.RoleKey && r.RoleId != role.RoleId && r.DelFlag == "0");

            if (duplicateKey)
            {
                return new ApiResult<SysRole> { Code = 400, Msg = $"权限字符 '{role.RoleKey}' 已存在" };
            }

            exists.RoleName = role.RoleName;
            exists.RoleKey = role.RoleKey;
            exists.RoleSort = role.RoleSort;
            exists.DataScope = role.DataScope;
            exists.MenuCheckStrictly = role.MenuCheckStrictly;
            exists.DeptCheckStrictly = role.DeptCheckStrictly;
            exists.Status = role.Status;
            exists.UpdateBy = AppHelper.GetCurrentUserName();
            exists.UpdateTime = DateTime.Now;
            exists.Remark = role.Remark;

            db.SysRoles.Update(exists);
            await db.SaveChangesAsync();

            return new ApiResult<SysRole> { Code = 200, Msg = "修改成功", Data = exists };
        }

        public async Task<ApiResult<int>> DeleteRoleAsync(long roleId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var role = await db.SysRoles
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                return new ApiResult<int> { Code = 400, Msg = "角色不存在", Data = 0 };
            }

            // 检查是否为管理员角色
            if (role.RoleKey == "admin")
            {
                return new ApiResult<int> { Code = 400, Msg = "管理员角色不能删除", Data = 0 };
            }

            // 检查是否有用户关联此角色
            var hasUsers = await db.SysUserRoles.AnyAsync(ur => ur.RoleId == roleId);
            if (hasUsers)
            {
                return new ApiResult<int> { Code = 400, Msg = "角色已分配给用户，不能删除", Data = 0 };
            }

            // 软删除角色时，同步删除角色与菜单的关联，避免残留导致菜单无法删除
            var roleMenus = await db.SysRoleMenus
                .Where(rm => rm.RoleId == roleId)
                .ToListAsync();
            if (roleMenus.Count > 0)
            {
                db.SysRoleMenus.RemoveRange(roleMenus);
            }

            // 软删除
            role.DelFlag = "2";
            db.SysRoles.Update(role);
            await db.SaveChangesAsync();

            return new ApiResult<int> { Code = 200, Msg = "删除成功", Data = 1 };
        }

        public async Task SwitchStatusAsync(long roleId, string status)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            var entity = await db.SysRoles.FindAsync(roleId);
            if (entity != null)
            {
                entity.Status = status;
                entity.UpdateBy = AppHelper.GetCurrentUserName();
                entity.UpdateTime = DateTime.Now;
                await db.SaveChangesAsync();
            }
        }

        public async Task<SysRole?> GetRoleByIdAsync(long roleId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            return await db.SysRoles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleId == roleId);
        }

        public async Task<List<long>> QueryMenuIdsByRoleIdAsync(long roleId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            return await db.SysRoleMenus.AsNoTracking()
                .Where(rm => rm.RoleId == roleId)
                .Select(rm => rm.MenuId)
                .ToListAsync();
        }

        public async Task UpdateRoleMenusAsync(long roleId, List<long> menuIds)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            // 先删除旧的菜单权限
            var existingMenus = await db.SysRoleMenus
                .Where(rm => rm.RoleId == roleId)
                .ToListAsync();

            db.SysRoleMenus.RemoveRange(existingMenus);

            // 添加新的菜单权限
            if (menuIds != null && menuIds.Count > 0)
            {
                var newMenus = menuIds.Select(menuId => new SysRoleMenu
                {
                    RoleId = roleId,
                    MenuId = menuId
                }).ToList();

                db.SysRoleMenus.AddRange(newMenus);
            }

            await db.SaveChangesAsync();
        }

        private static SysRoleVO MapToVO(SysRole r)
        {
            return new SysRoleVO
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                RoleKey = r.RoleKey,
                RoleSort = r.RoleSort,
                DataScope = r.DataScope,
                MenuCheckStrictly = r.MenuCheckStrictly,
                DeptCheckStrictly = r.DeptCheckStrictly,
                Status = r.Status,
                DelFlag = r.DelFlag,
                CreateBy = r.CreateBy,
                CreateTime = r.CreateTime,
                Remark = r.Remark
            };
        }
    }
}