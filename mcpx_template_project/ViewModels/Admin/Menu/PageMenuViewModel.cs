using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.ViewModels.Admin.Menu
{
    public partial class PageMenuViewModel : ObservableObject
    {
        private readonly IMenuService _menuService;
        private readonly IDialogService _dialogService;
        private readonly IViewModelFactory _viewModelFactory;

        public ObservableCollection<SysMenuVO> MenuTree { get; } = new();

        public int MenuTreeCount => MenuTree.Count;

        [ObservableProperty]
        private string _searchMenuName = string.Empty;

        [ObservableProperty]
        private string _searchStatus = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private SysMenuVO? _selectedMenu;

        public PageMenuViewModel(IMenuService menuService, IDialogService dialogService, IViewModelFactory viewModelFactory)
        {
            _menuService = menuService;
            _dialogService = dialogService;
            _viewModelFactory = viewModelFactory;
            _ = LoadDataAsync();
        }

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                var query = new SysMenuQuery
                {
                    MenuName = SearchMenuName,
                    Status = SearchStatus
                };

                var tree = await _menuService.QueryMenuTreeAsync(query);

                MenuTree.Clear();
                foreach (var menu in tree)
                {
                    MenuTree.Add(menu);
                }
                OnPropertyChanged(nameof(MenuTreeCount));
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载菜单数据失败：" + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            try
            {
                IsLoading = true;

                var query = new SysMenuQuery
                {
                    MenuName = SearchMenuName,
                    Status = SearchStatus
                };

                var tree = await _menuService.QueryMenuTreeAsync(query);

                MenuTree.Clear();
                foreach (var menu in tree)
                {
                    MenuTree.Add(menu);
                }
                OnPropertyChanged(nameof(MenuTreeCount));
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("查询菜单失败：" + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Reset()
        {
            SearchMenuName = string.Empty;
            SearchStatus = string.Empty;
            _ = SearchAsync();
        }

        [RelayCommand]
        private async Task AddMenu()
        {
            var addMenuVm = _viewModelFactory.Create<AddEditMenuViewModel>();

            var allMenus = await _menuService.QueryAllMenusAsync();
            var menuTree = BuildSimpleTree(allMenus);
            foreach (var node in menuTree)
            {
                addMenuVm.MenuTree.Add(node);
            }

            var result = _dialogService.ShowDialog(addMenuVm);
            if (result == true)
            {
                await addMenuVm.SaveAsync();
                _ = LoadDataAsync();
                NotifyMenuChanged();
            }
        }

        [RelayCommand]
        private async Task EditMenu(SysMenuVO? menu)
        {
            if (menu == null) return;

            var editMenuVm = _viewModelFactory.Create<AddEditMenuViewModel>(menu);

            var allMenus = await _menuService.QueryAllMenusAsync();
            var menuTree = BuildSimpleTree(allMenus);
            foreach (var node in menuTree)
            {
                editMenuVm.MenuTree.Add(node);
            }
            editMenuVm.FindAndSelectParent(menu.ParentId);

            var result = _dialogService.ShowDialog(editMenuVm);
            if (result == true)
            {
                await editMenuVm.SaveAsync();
                _ = LoadDataAsync();
                NotifyMenuChanged();
            }
        }

        [RelayCommand]
        private async Task DeleteMenu(SysMenuVO? menu)
        {
            if (menu == null) return;

            if (!_dialogService.Confirm($"确定删除菜单「{menu.MenuName}」吗？"))
                return;

            try
            {
                IsLoading = true;
                var result = await _menuService.DeleteMenuAsync(menu.MenuId);
                if (result.IsSuccess)
                {
                    _dialogService.ShowSuccess("删除菜单成功");
                    _ = LoadDataAsync();
                    NotifyMenuChanged();
                }
                else
                {
                    _dialogService.ShowError(result.Msg);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("删除菜单失败：" + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SwitchStatus(SysMenuVO item)
        {
            var newStatus = item.Status == "0" ? "1" : "0";
            await _menuService.SwitchStatusAsync(item.MenuId, newStatus);
            item.Status = newStatus;
        }

        [RelayCommand]
        private async Task AddChildMenu(SysMenuVO? menu)
        {
            if (menu == null) return;

            var addMenuVm = _viewModelFactory.Create<AddEditMenuViewModel>();

            var allMenus = await _menuService.QueryAllMenusAsync();
            var menuTree = BuildSimpleTree(allMenus);
            foreach (var node in menuTree)
            {
                addMenuVm.MenuTree.Add(node);
            }
            addMenuVm.FindAndSelectParent(menu.MenuId);

            var result = _dialogService.ShowDialog(addMenuVm);
            if (result == true)
            {
                await addMenuVm.SaveAsync();
                _ = LoadDataAsync();
                NotifyMenuChanged();
            }
        }

        private static void NotifyMenuChanged()
        {
            WeakReferenceMessenger.Default.Send(new MenuChangedMessage());
        }

        private static List<SysMenuVO> BuildSimpleTree(List<SysMenuVO> flatList)
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
                    parent.Children.Add(item);
                }
                else
                {
                    roots.Add(item);
                }
            }

            return roots;
        }

        partial void OnSelectedMenuChanged(SysMenuVO? value)
        {
        }
    }
}
