using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using mcpx_template_project.Configation;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.Service.Admin.impl
{
    public class AuthServiceImpl : IAuthService
    {
        private readonly IDbContextFactory<EFDbContext> _contextFactory;

        public AuthServiceImpl(IDbContextFactory<EFDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<bool> IsAdminAsync(long userId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var roleIds = await db.SysUserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (roleIds.Count == 0) return false;

            return await db.SysRoles
                .AnyAsync(r => roleIds.Contains(r.RoleId) && r.RoleKey == "admin" && r.Status == "0");
        }

        public async Task<List<string>> GetPermsAsync(long userId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var isAdmin = await IsAdminAsync(userId);

            if (isAdmin)
            {
                return await db.SysMenus
                    .Where(m => m.Status == "0" && m.Perms != null && m.Perms != "")
                    .Select(m => m.Perms!)
                    .Distinct()
                    .ToListAsync();
            }

            var roleIds = await db.SysUserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (roleIds.Count == 0)
                return new List<string>();

            var perms = await (
                from m in db.SysMenus
                join rm in db.SysRoleMenus on m.MenuId equals rm.MenuId
                where m.Status == "0" && roleIds.Contains(rm.RoleId)
                where m.Perms != null && m.Perms != ""
                select m.Perms
            ).Distinct().ToListAsync();

            return perms;
        }

        public async Task<List<SysMenuVO>> GetMenuTreeAsync(long userId)
        {
            using var db = await _contextFactory.CreateDbContextAsync();

            var isAdmin = await IsAdminAsync(userId);

            List<SysMenu> menus;

            if (isAdmin)
            {
                menus = await db.SysMenus
                    .Where(m => m.Status == "0" && m.Visible == "0")
                    .ToListAsync();
            }
            else
            {
                var roleIds = await db.SysUserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                if (roleIds.Count == 0)
                    return new List<SysMenuVO>();

                menus = await (
                    from m in db.SysMenus
                    join rm in db.SysRoleMenus on m.MenuId equals rm.MenuId
                    where m.Status == "0" && m.Visible == "0"
                    where roleIds.Contains(rm.RoleId)
                    select m
                ).ToListAsync();
            }

            var filteredMenus = menus
                .Where(m => m.MenuType != "F")
                .OrderBy(m => m.OrderNum)
                .ToList();

            return BuildMenuTree(filteredMenus, 0);
        }

        private List<SysMenuVO> BuildMenuTree(List<SysMenu> allMenus, long parentId)
        {
            var result = new List<SysMenuVO>();

            foreach (var menu in allMenus.Where(m => m.ParentId == parentId))
            {
                var vo = new SysMenuVO
                {
                    MenuId = menu.MenuId,
                    MenuName = menu.MenuName,
                    ParentId = menu.ParentId,
                    OrderNum = menu.OrderNum,
                    Component = menu.Component,
                    MenuType = menu.MenuType,
                    Visible = menu.Visible,
                    Status = menu.Status,
                    Perms = menu.Perms,
                    Icon = menu.Icon,
                    CreateBy = menu.CreateBy,
                    CreateTime = menu.CreateTime,
                    UpdateBy = menu.UpdateBy,
                    UpdateTime = menu.UpdateTime,
                    Remark = menu.Remark
                };

                var children = BuildMenuTree(allMenus, menu.MenuId);
                foreach (var child in children)
                {
                    vo.Children.Add(child);
                }

                result.Add(vo);
            }

            return result;
        }
    }
}
