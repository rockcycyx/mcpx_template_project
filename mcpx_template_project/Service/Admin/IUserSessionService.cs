using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.VO;

namespace mcpx_template_project.Service.Admin
{
    public interface IUserSessionService : INotifyPropertyChanged
    {
        bool IsLoggedIn { get; }

        SysUser? CurrentUser { get; }

        HashSet<string> Perms { get; }

        ObservableCollection<SysMenuVO> Menus { get; }

        bool IsAdmin { get; }

        void Login(SysUser user, HashSet<string> perms, List<SysMenuVO> menus, bool isAdmin);

        void Logout();

        /// <summary>登录状态下更新菜单集合（菜单管理变更后刷新侧边栏）</summary>
        void UpdateMenus(List<SysMenuVO> menus);

        /// <summary>
        /// 判断当前用户是否拥有指定权限标识（sys_menu.perms，F 类型按钮权限）；
        /// 管理员直接放行，权限标识为空视为无限制，未登录返回 false。
        /// </summary>
        bool HasPerm(string perm);
    }
}
