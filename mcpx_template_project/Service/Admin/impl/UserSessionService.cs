using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.Service.Admin.impl
{
    public partial class UserSessionService : ObservableObject, IUserSessionService
    {
        public bool IsLoggedIn { get; private set; }

        public SysUser? CurrentUser { get; private set; }

        public HashSet<string> Perms { get; private set; } = new();

        [ObservableProperty]
        private ObservableCollection<SysMenuVO> _menus = new();

        public bool IsAdmin { get; private set; }

        public void Login(SysUser user, HashSet<string> perms, List<SysMenuVO> menus, bool isAdmin)
        {
            CurrentUser = user;
            Perms = perms;
            Menus = new ObservableCollection<SysMenuVO>(menus);
            IsAdmin = isAdmin;
            IsLoggedIn = true;
        }

        public void Logout()
        {
            CurrentUser = null;
            Perms = new HashSet<string>();
            Menus = new ObservableCollection<SysMenuVO>();
            IsAdmin = false;
            IsLoggedIn = false;
        }

        public void UpdateMenus(List<SysMenuVO> menus)
        {
            if (!IsLoggedIn) return;
            Menus = new ObservableCollection<SysMenuVO>(menus);
        }

        public bool HasPerm(string perm)
        {
            if (string.IsNullOrWhiteSpace(perm)) return true;
            if (!IsLoggedIn) return false;
            return IsAdmin || Perms.Contains(perm);
        }
    }
}
