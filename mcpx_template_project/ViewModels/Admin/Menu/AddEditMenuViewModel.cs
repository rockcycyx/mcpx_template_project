using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.ViewModels.Admin.Menu
{
    public partial class AddEditMenuViewModel : ObservableObject
    {
        private readonly IMenuService _menuService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _menuName = string.Empty;

        [ObservableProperty]
        private string _menuType = "C";

        [ObservableProperty]
        private int _orderNum;

        [ObservableProperty]
        private long? _parentId;

        [ObservableProperty]
        private string _component = string.Empty;

        [ObservableProperty]
        private string _visible = "0";

        [ObservableProperty]
        private string _status = "0";

        [ObservableProperty]
        private string _perms = string.Empty;

        [ObservableProperty]
        private string _icon = string.Empty;

        [ObservableProperty]
        private string _remark = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private SysMenuVO? _selectedParentMenu;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private bool _isStatusActive = true;

        partial void OnIsStatusActiveChanged(bool value) => Status = value ? "0" : "1";

        [ObservableProperty]
        private long _editingMenuId;

        public ObservableCollection<SysMenuVO> MenuTree { get; } = new();

        public SysMenuVO? EditingMenu { get; }

        public string SelectedParentMenuName => SelectedParentMenu?.MenuName ?? "请选择上级菜单";

        public event Action<bool>? RequestClose;

        public AddEditMenuViewModel(IMenuService menuService, IDialogService dialogService)
        {
            _menuService = menuService;
            _dialogService = dialogService;
        }

        public AddEditMenuViewModel(SysMenuVO editingMenu, IMenuService menuService, IDialogService dialogService)
        {
            _menuService = menuService;
            _dialogService = dialogService;
            EditingMenu = editingMenu;
            IsEditMode = true;
            EditingMenuId = editingMenu.MenuId;

            MenuName = editingMenu.MenuName ?? string.Empty;
            MenuType = editingMenu.MenuType ?? "C";
            OrderNum = editingMenu.OrderNum ?? 0;
            ParentId = editingMenu.ParentId;
            Component = editingMenu.Component ?? string.Empty;
            Visible = editingMenu.Visible ?? "0";
            Status = editingMenu.Status ?? "0";
            IsStatusActive = editingMenu.Status != "1";
            Perms = editingMenu.Perms ?? string.Empty;
            Icon = editingMenu.Icon ?? string.Empty;
            Remark = editingMenu.Remark ?? string.Empty;
        }

        public Visibility IsTypeMVisibility => MenuType == "M" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsTypeCVisibility => MenuType == "C" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsTypeFVisibility => MenuType == "F" ? Visibility.Visible : Visibility.Collapsed;

        public Visibility IsComponentVisibility => MenuType == "C" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsVisibleVisibility => MenuType == "M" || MenuType == "C" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsPermsVisibility => MenuType == "C" || MenuType == "F" ? Visibility.Visible : Visibility.Collapsed;

        partial void OnMenuTypeChanged(string value)
        {
            OnPropertyChanged(nameof(IsTypeMVisibility));
            OnPropertyChanged(nameof(IsTypeCVisibility));
            OnPropertyChanged(nameof(IsTypeFVisibility));
            OnPropertyChanged(nameof(IsComponentVisibility));
            OnPropertyChanged(nameof(IsVisibleVisibility));
            OnPropertyChanged(nameof(IsPermsVisibility));
        }

        partial void OnSelectedParentMenuChanged(SysMenuVO? value)
        {
            OnPropertyChanged(nameof(SelectedParentMenuName));
        }

        [RelayCommand]
        private void SelectParentMenu(SysMenuVO? menu)
        {
            if (menu == null)
            {
                SelectedParentMenu = null;
                return;
            }

            if (IsEditMode && EditingMenu != null && menu.MenuId == EditingMenu.MenuId)
            {
                ErrorMessage = "不能将自身设为上级菜单";
                return;
            }

            SelectedParentMenu = menu;
            ParentId = menu.MenuId;
            ClearError();
        }

        [RelayCommand]
        private void ClearParentMenu()
        {
            SelectedParentMenu = null;
            ParentId = null;
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(MenuName))
            {
                ErrorMessage = "菜单名称不能为空";
                return;
            }

            if (OrderNum < 0)
            {
                ErrorMessage = "显示排序不能为负数";
                return;
            }

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        private void ClearError()
        {
            ErrorMessage = string.Empty;
        }

        public void FindAndSelectParent(long? parentId)
        {
            if (!parentId.HasValue) return;
            var menu = FindMenuById(MenuTree, parentId.Value);
            if (menu != null)
            {
                SelectedParentMenu = menu;
                ParentId = parentId.Value;
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

        public SysMenu ToEntity()
        {
            return new SysMenu
            {
                MenuId = IsEditMode ? EditingMenuId : 0,
                MenuName = MenuName,
                MenuType = MenuType,
                OrderNum = OrderNum,
                // 目录（M）固定为顶级；其余类型未选择上级菜单时也归入顶级（parent_id=0），避免 NULL 导致菜单树丢失
                ParentId = MenuType == "M" ? 0 : (ParentId ?? 0),
                Component = MenuType == "C" ? Component : null,
                Visible = Visible,
                Status = Status,
                Perms = (MenuType == "C" || MenuType == "F") ? Perms : null,
                Icon = string.IsNullOrWhiteSpace(Icon) ? null : Icon,
                Remark = Remark
            };
        }

        public async Task SaveAsync()
        {
            try
            {
                var entity = ToEntity();
                var apiResult = IsEditMode
                    ? await _menuService.UpdateMenuAsync(entity)
                    : await _menuService.InsertMenuAsync(entity);

                if (apiResult.IsSuccess)
                    _dialogService.ShowSuccess(IsEditMode ? "修改菜单成功" : "新增菜单成功");
                else
                    _dialogService.ShowError(apiResult.Msg);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError((IsEditMode ? "修改" : "新增") + "菜单失败：" + ex.Message);
            }
        }
    }
}