using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.ViewModels.Common;

namespace mcpx_template_project.ViewModels.Admin.Role
{
    public partial class PageRoleVIewModel : PagedViewModelBase<SysRoleVO, SysRoleQuery>
    {
        private readonly IRoleService _roleService;
        private readonly IMenuService _menuService;
        private readonly IViewModelFactory _viewModelFactory;

        public ObservableCollection<SysRoleVO> RoleList => Items;

        [ObservableProperty]
        private string _searchRoleName = string.Empty;

        [ObservableProperty]
        private string _searchRoleKey = string.Empty;

        [ObservableProperty]
        private string _searchStatus = string.Empty;

        [ObservableProperty]
        private DateTime? _searchBeginTime;

        [ObservableProperty]
        private DateTime? _searchEndTime;

        [ObservableProperty]
        private SysRoleVO? _selectedRole;

        [ObservableProperty]
        private bool _isAllSelected;

        protected override string LoadErrorMessage => "加载角色失败";

        public PageRoleVIewModel(IRoleService roleService, IMenuService menuService, IDialogService dialogService, IViewModelFactory viewModelFactory)
            : base(dialogService)
        {
            _roleService = roleService;
            _menuService = menuService;
            _viewModelFactory = viewModelFactory;
            _ = RefreshAsync();
        }

        private bool _isSyncingSelectAll;

        private void UpdateSelectAllState()
        {
            _isSyncingSelectAll = true;
            IsAllSelected = RoleList.Count > 0 && RoleList.All(r => r.IsSelected);
            _isSyncingSelectAll = false;
        }

        protected override SysRoleQuery BuildQuery() => new()
        {
            RoleName = SearchRoleName,
            RoleKey = SearchRoleKey,
            Status = SearchStatus,
            BeginTime = SearchBeginTime,
            EndTime = SearchEndTime
        };

        protected override Task<PagedResult<SysRoleVO>> QueryPagedAsync(SysRoleQuery query, PagingParameters paging)
            => _roleService.QueryRolesPagedAsync(query, paging);

        protected override void ResetSearchCore()
        {
            SearchRoleName = string.Empty;
            SearchRoleKey = string.Empty;
            SearchStatus = string.Empty;
            SearchBeginTime = null;
            SearchEndTime = null;
        }

        protected override void AttachItem(SysRoleVO item) => item.PropertyChanged += Role_PropertyChanged;

        protected override void DetachItem(SysRoleVO item) => item.PropertyChanged -= Role_PropertyChanged;

        protected override void OnItemsLoaded() => UpdateSelectAllState();

        [RelayCommand]
        private void SelectAll()
        {
            bool targetState = RoleList.Count > 0 && !RoleList.All(r => r.IsSelected);
            _isSyncingSelectAll = true;
            foreach (var role in RoleList)
            {
                if (!role.IsAdmin)
                    role.IsSelected = targetState;
            }
            _isSyncingSelectAll = false;
            UpdateSelectAllState();
        }

        [RelayCommand]
        private void MoreAction(SysRoleVO? role)
        {
            if (role == null) return;
            DialogService.ShowInfo($"更多操作：{role.RoleName}（功能待实现）");
        }

        [RelayCommand]
        private async Task AddRole()
        {
            var menus = await _menuService.QueryMenuTreeAsync(new SysMenuQuery());
            var menuTree = new ObservableCollection<SysMenuVO>(menus);

            var vm = _viewModelFactory.Create<AddEditRoleViewModel>();
            vm.SetMenuTree(menuTree);

            var result = DialogService.ShowDialog(vm);
            if (result == true)
            {
                await vm.SaveAsync();
                _ = RefreshAsync();
            }
        }

        [RelayCommand]
        private async Task EditRole(SysRoleVO? role)
        {
            if (role == null) return;

            var menus = await _menuService.QueryMenuTreeAsync(new SysMenuQuery());
            var menuTree = new ObservableCollection<SysMenuVO>(menus);

            var menuIds = await _roleService.QueryMenuIdsByRoleIdAsync(role.RoleId);

            var vm = _viewModelFactory.Create<AddEditRoleViewModel>(role);
            vm.SetMenuTree(menuTree, menuIds);

            var result = DialogService.ShowDialog(vm);
            if (result == true)
            {
                await vm.SaveAsync();
                _ = RefreshAsync();
            }
        }

        [RelayCommand]
        private async Task DeleteRole(SysRoleVO? role)
        {
            if (role == null) return;
            if (role.IsAdmin)
            {
                DialogService.ShowWarning("管理员角色不能删除");
                return;
            }

            var confirm = DialogService.Confirm($"确定删除角色「{role.RoleName}」吗？");
            if (!confirm) return;

            try
            {
                var result = await _roleService.DeleteRoleAsync(role.RoleId);
                if (result.Code == 200)
                {
                    DialogService.ShowSuccess("删除成功");
                    _ = RefreshAsync();
                }
                else
                {
                    DialogService.ShowError(result.Msg);
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowError("删除角色失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task BatchDelete()
        {
            var selectedRoles = RoleList.Where(r => r.IsSelected).ToList();
            if (selectedRoles.Count == 0)
            {
                DialogService.ShowWarning("请先选择要删除的角色");
                return;
            }

            if (selectedRoles.Any(r => r.IsAdmin))
            {
                DialogService.ShowWarning("管理员角色不能删除");
                return;
            }

            var confirm = DialogService.Confirm($"确定删除选中的 {selectedRoles.Count} 个角色吗？");
            if (!confirm) return;

            try
            {
                foreach (var role in selectedRoles)
                {
                    var result = await _roleService.DeleteRoleAsync(role.RoleId);
                    if (result.Code != 200)
                    {
                        DialogService.ShowError($"删除角色「{role.RoleName}」失败：{result.Msg}");
                        return;
                    }
                }
                DialogService.ShowSuccess("批量删除成功");
                _ = RefreshAsync();
            }
            catch (Exception ex)
            {
                DialogService.ShowError("批量删除失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private void Import()
        {
            DialogService.ShowInfo("导入（功能待实现）");
        }

        [RelayCommand]
        private void Export()
        {
            DialogService.ShowInfo("导出（功能待实现）");
        }

        [RelayCommand]
        private async Task SwitchStatus(SysRoleVO item)
        {
            var newStatus = item.Status == "0" ? "1" : "0";
            await _roleService.SwitchStatusAsync(item.RoleId, newStatus);
            item.Status = newStatus;
        }

        partial void OnSelectedRoleChanged(SysRoleVO? value)
        {
        }

        private void Role_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SysRoleVO.IsSelected) && !_isSyncingSelectAll)
            {
                UpdateSelectAllState();
            }
        }
    }
}
