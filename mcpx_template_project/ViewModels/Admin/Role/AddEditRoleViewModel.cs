using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.ViewModels.Admin.Role
{
    public partial class AddEditRoleViewModel : ObservableObject
    {
        private readonly IRoleService _roleService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _roleName = string.Empty;

        [ObservableProperty]
        private string _roleKey = string.Empty;

        [ObservableProperty]
        private int _roleSort;

        [ObservableProperty]
        private string _status = "0";

        [ObservableProperty]
        private string _remark = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private bool _isStatusActive = true;

        partial void OnIsStatusActiveChanged(bool value) => Status = value ? "0" : "1";

        [ObservableProperty]
        private long _editingRoleId;

        [ObservableProperty]
        private bool _isExpandAll = true;

        [ObservableProperty]
        private bool _isAllSelected;

        [ObservableProperty]
        private bool _isCheckStrictly = true;

        private bool _isSyncing;

        public ObservableCollection<SysMenuVO> MenuTree { get; } = new();

        public SysRoleVO? EditingRole { get; }

        public event Action<bool>? RequestClose;

        public AddEditRoleViewModel(IRoleService roleService, IDialogService dialogService)
        {
            _roleService = roleService;
            _dialogService = dialogService;
        }

        public AddEditRoleViewModel(SysRoleVO editingRole, IRoleService roleService, IDialogService dialogService)
        {
            _roleService = roleService;
            _dialogService = dialogService;
            EditingRole = editingRole;
            IsEditMode = true;
            EditingRoleId = editingRole.RoleId;

            RoleName = editingRole.RoleName ?? string.Empty;
            RoleKey = editingRole.RoleKey ?? string.Empty;
            RoleSort = editingRole.RoleSort;
            Status = editingRole.Status ?? "0";
            IsStatusActive = editingRole.Status != "1";
            Remark = editingRole.Remark ?? string.Empty;
        }

        /// <summary>
        /// 设置菜单树并回显已选菜单
        /// </summary>
        public void SetMenuTree(ObservableCollection<SysMenuVO> menuTree, List<long>? selectedMenuIds = null)
        {
            MenuTree.Clear();
            foreach (var item in menuTree)
            {
                MenuTree.Add(item);
            }

            if (selectedMenuIds != null && selectedMenuIds.Count > 0)
            {
                foreach (var menuId in selectedMenuIds)
                {
                    var menu = FindMenuById(MenuTree, menuId);
                    if (menu != null)
                    {
                        menu.IsSelected = true;
                    }
                }
            }

            UpdateSelectAllState();
        }

        /// <summary>
        /// 展开/折叠所有节点
        /// </summary>
        partial void OnIsExpandAllChanged(bool value)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            SetExpandState(MenuTree, value);
            _isSyncing = false;
        }

        /// <summary>
        /// 全选/全不选
        /// </summary>
        partial void OnIsAllSelectedChanged(bool value)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            SetAllSelected(MenuTree, value);
            _isSyncing = false;
        }

        /// <summary>
        /// 菜单项勾选变化时触发（用于父子联动）
        /// </summary>
        public void OnMenuCheckedChanged(SysMenuVO? menu)
        {
            if (menu == null || _isSyncing) return;

            if (IsCheckStrictly)
            {
                _isSyncing = true;

                if (menu.IsSelected)
                {
                    // 双向联动：选中时自动选中所有子节点（向下）和父节点（向上）
                    UpdateChildrenState(menu, true);
                    SelectParentNodes(MenuTree, menu);
                }
                else
                {
                    // 取消选中时自动取消所有子节点
                    UpdateChildrenState(menu, false);
                }

                _isSyncing = false;
            }

            UpdateSelectAllState();
        }

        private void UpdateSelectAllState()
        {
            _isSyncing = true;
            IsAllSelected = MenuTree.Any() && !HasUnselectedMenu(MenuTree);
            _isSyncing = false;
        }

        private bool HasUnselectedMenu(ObservableCollection<SysMenuVO> nodes)
        {
            foreach (var node in nodes)
            {
                if (!node.IsSelected) return true;
                if (HasUnselectedMenu(node.Children)) return true;
            }
            return false;
        }

        private void SetAllSelected(ObservableCollection<SysMenuVO> nodes, bool selected)
        {
            foreach (var node in nodes)
            {
                node.IsSelected = selected;
                SetAllSelected(node.Children, selected);
            }
        }

        private void SetExpandState(ObservableCollection<SysMenuVO> nodes, bool expanded)
        {
            foreach (var node in nodes)
            {
                node.IsExpanded = expanded;
                SetExpandState(node.Children, expanded);
            }
        }

        /// <summary>
        /// 递归更新子节点选中状态
        /// </summary>
        private void UpdateChildrenState(SysMenuVO parent, bool selected)
        {
            foreach (var child in parent.Children)
            {
                child.IsSelected = selected;
                UpdateChildrenState(child, selected);
            }
        }

        /// <summary>
        /// 选中子节点时自动选中所有父节点
        /// </summary>
        private void SelectParentNodes(ObservableCollection<SysMenuVO> nodes, SysMenuVO target)
        {
            foreach (var node in nodes)
            {
                if (node.MenuId == target.MenuId) continue;

                if (node.Children.Contains(target))
                {
                    if (!node.IsSelected)
                    {
                        node.IsSelected = true;
                    }
                    // 继续向上查找
                    SelectParentNodes(MenuTree, node);
                }
                else
                {
                    SelectParentNodes(node.Children, target);
                }
            }
        }

        private static SysMenuVO? FindMenuById(ObservableCollection<SysMenuVO> nodes, long menuId)
        {
            foreach (var node in nodes)
            {
                if (node.MenuId == menuId)
                    return node;

                var found = FindMenuById(node.Children, menuId);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// 获取所有选中的菜单ID（包括子节点）
        /// </summary>
        public List<long> GetSelectedMenuIds()
        {
            var result = new List<long>();
            CollectSelectedMenuIds(MenuTree, result);
            return result;
        }

        private static void CollectSelectedMenuIds(ObservableCollection<SysMenuVO> nodes, List<long> result)
        {
            foreach (var node in nodes)
            {
                if (node.IsSelected)
                {
                    result.Add(node.MenuId);
                }
                CollectSelectedMenuIds(node.Children, result);
            }
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(RoleName))
            {
                ErrorMessage = "角色名称不能为空";
                return;
            }

            if (string.IsNullOrWhiteSpace(RoleKey))
            {
                ErrorMessage = "权限字符不能为空";
                return;
            }

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        public SysRole ToEntity()
        {
            return new SysRole
            {
                RoleId = IsEditMode ? EditingRoleId : 0,
                RoleName = RoleName,
                RoleKey = RoleKey,
                RoleSort = RoleSort,
                Status = Status,
                Remark = Remark,
                DelFlag = "0"
            };
        }

        public async Task SaveAsync()
        {
            try
            {
                var roleEntity = ToEntity();
                var selectedMenuIds = GetSelectedMenuIds();

                if (IsEditMode)
                {
                    var saved = await _roleService.UpdateRoleAsync(roleEntity);
                    if (saved.Code == 200)
                    {
                        await _roleService.UpdateRoleMenusAsync(EditingRoleId, selectedMenuIds);
                        _dialogService.ShowSuccess("修改角色成功");
                    }
                    else
                    {
                        _dialogService.ShowError(saved.Msg);
                    }
                }
                else
                {
                    var saved = await _roleService.InsertRoleAsync(roleEntity);
                    if (saved.Code == 200 && saved.Data != null)
                    {
                        await _roleService.UpdateRoleMenusAsync(saved.Data.RoleId, selectedMenuIds);
                        _dialogService.ShowSuccess("新增角色成功");
                    }
                    else
                    {
                        _dialogService.ShowError(saved.Msg);
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError((IsEditMode ? "修改" : "新增") + "角色失败：" + ex.Message);
            }
        }
    }
}
