using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.ViewModels.Admin.Dept
{
    public partial class PageDeptViewModel : ObservableObject
    {
        private readonly IDeptService _deptService;
        private readonly IDialogService _dialogService;
        private readonly IViewModelFactory _viewModelFactory;

        public ObservableCollection<SysDeptVO> DeptTree { get; } = new();

        [ObservableProperty]
        private string _searchDeptName = string.Empty;

        [ObservableProperty]
        private string _searchStatus = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private SysDeptVO? _selectedDept;

        public PageDeptViewModel(IDeptService deptService, IDialogService dialogService, IViewModelFactory viewModelFactory)
        {
            _deptService = deptService;
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

                var query = new SysDeptQuery
                {
                    DeptName = SearchDeptName,
                    Status = SearchStatus
                };

                var tree = await _deptService.QueryDeptTreeAsync(query);

                DeptTree.Clear();
                foreach (var dept in tree)
                {
                    DeptTree.Add(dept);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载部门数据失败：" + ex.Message);
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

                var query = new SysDeptQuery
                {
                    DeptName = SearchDeptName,
                    Status = SearchStatus
                };

                var tree = await _deptService.QueryDeptTreeAsync(query);

                DeptTree.Clear();
                foreach (var dept in tree)
                {
                    DeptTree.Add(dept);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("查询部门失败：" + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Reset()
        {
            SearchDeptName = string.Empty;
            SearchStatus = string.Empty;
            _ = SearchAsync();
        }

        [RelayCommand]
        private async Task AddDept()
        {
            var addDeptVm = _viewModelFactory.Create<AddDeptViewModel>();

            var tree = await _deptService.QueryDeptTreeAsync(new SysDeptQuery());
            foreach (var node in tree)
            {
                addDeptVm.DeptTree.Add(node);
            }

            var result = _dialogService.ShowDialog(addDeptVm);
            if (result == true)
            {
                await addDeptVm.SaveAsync();
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private async Task EditDept(SysDeptVO? dept)
        {
            if (dept == null) return;

            var editDeptVm = _viewModelFactory.Create<AddDeptViewModel>(dept);

            var tree = await _deptService.QueryDeptTreeAsync(new SysDeptQuery());
            foreach (var node in tree)
            {
                editDeptVm.DeptTree.Add(node);
            }

            if (dept.ParentId.HasValue)
            {
                editDeptVm.FindAndSelectParent(dept.ParentId.Value);
            }

            var result = _dialogService.ShowDialog(editDeptVm);
            if (result == true)
            {
                await editDeptVm.SaveAsync();
                await LoadDataAsync();
            }
        }

        [RelayCommand]
        private async Task DeleteDept(SysDeptVO? dept)
        {
            if (dept == null) return;

            var confirm = _dialogService.Confirm($"确定删除部门「{dept.DeptName}」吗？");
            if (!confirm) return;

            try
            {
                var result = await _deptService.DeleteDeptAsync(dept.DeptId);
                if (result.Code == 200)
                {
                    _dialogService.ShowSuccess("删除成功");
                    await LoadDataAsync();
                }
                else
                {
                    _dialogService.ShowError(result.Msg);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("删除部门失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task SwitchStatus(SysDeptVO item)
        {
            var newStatus = item.Status == "0" ? "1" : "0";
            await _deptService.SwitchStatusAsync(item.DeptId, newStatus);
            item.Status = newStatus;
        }

        [RelayCommand]
        private async Task AddChildDept(SysDeptVO? dept)
        {
            if (dept == null) return;

            var addDeptVm = _viewModelFactory.Create<AddDeptViewModel>();

            var tree = await _deptService.QueryDeptTreeAsync(new SysDeptQuery());
            foreach (var node in tree)
            {
                addDeptVm.DeptTree.Add(node);
            }

            addDeptVm.FindAndSelectParent(dept.DeptId);

            var result = _dialogService.ShowDialog(addDeptVm);
            if (result == true)
            {
                await addDeptVm.SaveAsync();
                await LoadDataAsync();
            }
        }

        partial void OnSelectedDeptChanged(SysDeptVO? value)
        {
        }
    }
}
