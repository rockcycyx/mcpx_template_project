using Microsoft.EntityFrameworkCore;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Helper;

namespace mcpx_template_project.Service.Admin.impl
{
    public class MenuServiceImpl : IMenuService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public MenuServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<SysMenuVO>> QueryMenuListAsync(SysMenuQuery query)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var queryable = db.SysMenus.AsNoTracking();

            if (query.MenuId.HasValue && query.MenuId.Value != 0)
            {
                queryable = queryable.Where(m => m.MenuId == query.MenuId.Value);
            }

            if (query.ParentId.HasValue && query.ParentId.Value != 0)
            {
                queryable = queryable.Where(m => m.ParentId == query.ParentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.MenuName))
            {
                queryable = queryable.Where(m =>
                    EF.Functions.Like(m.MenuName, $"%{query.MenuName}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                queryable = queryable.Where(m => m.Status == query.Status);
            }

            if (!string.IsNullOrWhiteSpace(query.MenuType))
            {
                queryable = queryable.Where(m => m.MenuType == query.MenuType);
            }

            var entities = await queryable
                .OrderBy(m => m.ParentId)
                .ThenBy(m => m.OrderNum)
                .ToListAsync();

            return entities.Select(MapToVO).ToList();
        }

        public async Task<List<SysMenuVO>> QueryMenuTreeAsync(SysMenuQuery query)
        {
            var flatList = await QueryMenuListAsync(query);
            return BuildTree(flatList);
        }

        public async Task<List<SysMenuVO>> QueryAllMenusAsync()
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var entities = await db.SysMenus.AsNoTracking()
                .OrderBy(m => m.ParentId)
                .ThenBy(m => m.OrderNum)
                .ToListAsync();

            return entities.Select(MapToVO).ToList();
        }

        public async Task<SysMenu?> GetMenuByIdAsync(long menuId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            return await db.SysMenus.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MenuId == menuId);
        }

        public async Task<ApiResult<SysMenu>> InsertMenuAsync(SysMenu menu)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            if (string.IsNullOrWhiteSpace(menu.MenuName))
            {
                return new ApiResult<SysMenu> { Code = 400, Msg = "菜单名称不能为空" };
            }

            if (string.IsNullOrWhiteSpace(menu.MenuType))
            {
                return new ApiResult<SysMenu> { Code = 400, Msg = "菜单类型不能为空" };
            }

            // 校验同级下菜单名称唯一
            var exists = await db.SysMenus
                .AnyAsync(m => m.ParentId == menu.ParentId && m.MenuName == menu.MenuName);

            if (exists)
            {
                return new ApiResult<SysMenu> { Code = 400, Msg = $"同级下已存在菜单名称 '{menu.MenuName}'" };
            }

            // 校验不能将自己的子孙设为父菜单
            if (menu.ParentId.HasValue && menu.ParentId.Value != 0)
            {
                var parent = await db.SysMenus.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MenuId == menu.ParentId.Value);

                if (parent == null)
                {
                    return new ApiResult<SysMenu> { Code = 400, Msg = "父菜单不存在" };
                }
            }

            menu.CreateTime = DateTime.Now;
            menu.CreateBy = AppHelper.GetCurrentUserName();
            menu.Status ??= "0";
            menu.Visible ??= "0";
            menu.MenuType ??= "C";
            menu.OrderNum ??= 0;

            db.SysMenus.Add(menu);
            await db.SaveChangesAsync();

            return new ApiResult<SysMenu> { Code = 200, Msg = "新增成功", Data = menu };
        }

        public async Task<ApiResult<SysMenu>> UpdateMenuAsync(SysMenu menu)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var exists = await db.SysMenus
                .FirstOrDefaultAsync(m => m.MenuId == menu.MenuId);

            if (exists == null)
            {
                return new ApiResult<SysMenu> { Code = 400, Msg = "菜单不存在" };
            }

            if (string.IsNullOrWhiteSpace(menu.MenuName))
            {
                return new ApiResult<SysMenu> { Code = 400, Msg = "菜单名称不能为空" };
            }

            // 校验同级下菜单名称唯一（排除自身）
            var duplicateName = await db.SysMenus
                .AnyAsync(m => m.ParentId == menu.ParentId && m.MenuId != menu.MenuId && m.MenuName == menu.MenuName);

            if (duplicateName)
            {
                return new ApiResult<SysMenu> { Code = 400, Msg = $"同级下已存在菜单名称 '{menu.MenuName}'" };
            }

            // 校验不能将自己的子孙设为父菜单
            if (menu.ParentId.HasValue && menu.ParentId.Value != 0)
            {
                if (menu.ParentId.Value == menu.MenuId)
                {
                    return new ApiResult<SysMenu> { Code = 400, Msg = "不能将自身设为父菜单" };
                }

                var isDescendant = await IsDescendantAsync(db, menu.MenuId, menu.ParentId.Value);
                if (isDescendant)
                {
                    return new ApiResult<SysMenu> { Code = 400, Msg = "不能将子孙设为父菜单" };
                }
            }

            exists.MenuName = menu.MenuName;
            exists.ParentId = menu.ParentId;
            exists.OrderNum = menu.OrderNum;
            exists.Component = menu.Component;
            exists.MenuType = menu.MenuType;
            exists.Visible = menu.Visible;
            exists.Status = menu.Status;
            exists.Perms = menu.Perms;
            exists.Icon = menu.Icon;
            exists.Remark = menu.Remark;
            exists.UpdateBy = AppHelper.GetCurrentUserName();
            exists.UpdateTime = DateTime.Now;

            db.SysMenus.Update(exists);
            await db.SaveChangesAsync();

            return new ApiResult<SysMenu> { Code = 200, Msg = "修改成功", Data = exists };
        }

        public async Task<ApiResult<int>> DeleteMenuAsync(long menuId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var menu = await db.SysMenus
                .FirstOrDefaultAsync(m => m.MenuId == menuId);

            if (menu == null)
            {
                return new ApiResult<int> { Code = 400, Msg = "菜单不存在", Data = 0 };
            }

            // 检查是否有子菜单
            var hasChildren = await db.SysMenus.AnyAsync(m => m.ParentId == menuId);
            if (hasChildren)
            {
                return new ApiResult<int> { Code = 400, Msg = "存在子菜单，不能删除", Data = 0 };
            }

            // 检查是否已分配给角色
            var isAssigned = await db.SysRoleMenus.AnyAsync(rm => rm.MenuId == menuId);
            if (isAssigned)
            {
                return new ApiResult<int> { Code = 400, Msg = "菜单已分配给角色，不能删除", Data = 0 };
            }

            db.SysMenus.Remove(menu);
            await db.SaveChangesAsync();

            return new ApiResult<int> { Code = 200, Msg = "删除成功", Data = 1 };
        }

        public async Task SwitchStatusAsync(long menuId, string status)
        {
            using var db = await _contextFactory.CreateDbContextAsync();
            var entity = await db.SysMenus.FindAsync(menuId);
            if (entity != null)
            {
                entity.Status = status;
                entity.UpdateBy = AppHelper.GetCurrentUserName();
                entity.UpdateTime = DateTime.Now;
                await db.SaveChangesAsync();
            }
        }

        private async Task<bool> IsDescendantAsync(EFDbContext db, long ancestorId, long candidateId)
        {
            var children = await db.SysMenus.AsNoTracking()
                .Where(m => m.ParentId == ancestorId)
                .Select(m => m.MenuId)
                .ToListAsync();

            foreach (var childId in children)
            {
                if (childId == candidateId) return true;
                var isDesc = await IsDescendantAsync(db, childId, candidateId);
                if (isDesc) return true;
            }

            return false;
        }

        private static List<SysMenuVO> BuildTree(List<SysMenuVO> flatList)
        {
            var lookup = new Dictionary<long, SysMenuVO>();
            var roots = new List<SysMenuVO>();

            foreach (var item in flatList)
            {
                lookup[item.MenuId] = item;
            }

            foreach (var item in flatList)
            {
                if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parent))
                {
                    item.ParentName = parent.MenuName;
                    parent.Children.Add(item);
                }
                else
                {
                    roots.Add(item);
                }
            }

            return roots;
        }

        private static SysMenuVO MapToVO(SysMenu m)
        {
            return new SysMenuVO
            {
                MenuId = m.MenuId,
                MenuName = m.MenuName,
                ParentId = m.ParentId,
                OrderNum = m.OrderNum,
                Component = m.Component,
                MenuType = m.MenuType,
                Visible = m.Visible,
                Status = m.Status,
                Perms = m.Perms,
                Icon = m.Icon,
                CreateBy = m.CreateBy,
                CreateTime = m.CreateTime,
                UpdateBy = m.UpdateBy,
                UpdateTime = m.UpdateTime,
                Remark = m.Remark
            };
        }
    }
}