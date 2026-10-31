using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.ViewModels.Admin.Dept
{
    public partial class AddDeptViewModel : ObservableObject
    {
        private readonly IDeptService _deptService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _deptName = string.Empty;

        [ObservableProperty]
        private int _orderNum;

        [ObservableProperty]
        private string _leader = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _status = "0";

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private SysDeptVO? _selectedParentDept;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private bool _isStatusActive = true;

        partial void OnIsStatusActiveChanged(bool value) => Status = value ? "0" : "1";

        public ObservableCollection<SysDeptVO> DeptTree { get; } = new();

        public SysDeptVO? EditingDept { get; }

        public string SelectedParentDeptName => SelectedParentDept?.DeptName ?? "请选择上级部门";

        public event Action<bool>? RequestClose;

        public AddDeptViewModel(IDeptService deptService, IDialogService dialogService)
        {
            _deptService = deptService;
            _dialogService = dialogService;
        }

        public AddDeptViewModel(SysDeptVO editingDept, IDeptService deptService, IDialogService dialogService)
        {
            _deptService = deptService;
            _dialogService = dialogService;
            EditingDept = editingDept;
            IsEditMode = true;

            DeptName = editingDept.DeptName;
            OrderNum = editingDept.OrderNum ?? 0;
            Leader = editingDept.Leader ?? string.Empty;
            Phone = editingDept.Phone ?? string.Empty;
            Email = editingDept.Email ?? string.Empty;
            Status = editingDept.Status ?? "0";
            IsStatusActive = editingDept.Status != "1";
        }

        [RelayCommand]
        private void SelectParentDept(SysDeptVO? dept)
        {
            if (dept == null)
            {
                SelectedParentDept = null;
                return;
            }

            if (IsEditMode && EditingDept != null && dept.DeptId == EditingDept.DeptId)
            {
                ErrorMessage = "不能将自身设为上级部门";
                return;
            }

            // 检查是否为子部门（防止循环引用）
            if (IsDescendant(dept))
            {
                ErrorMessage = "不能将子部门设为上级部门";
                return;
            }

            SelectedParentDept = dept;
            ClearError();
        }

        [RelayCommand]
        private void ClearParentDept()
        {
            SelectedParentDept = null;
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(DeptName))
            {
                ErrorMessage = "部门名称不能为空";
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

        public void FindAndSelectParent(long parentId)
        {
            var dept = FindDeptById(DeptTree, parentId);
            if (dept != null)
            {
                SelectedParentDept = dept;
            }
        }

        /// <summary>
        /// 判断选中的上级部门是否为当前编辑部门的子部门
        /// </summary>
        private bool IsDescendant(SysDeptVO candidate)
        {
            if (EditingDept == null) return false;
            return IsDescendantRecursive(EditingDept.Children, candidate.DeptId);
        }

        private static bool IsDescendantRecursive(ObservableCollection<SysDeptVO> children, long deptId)
        {
            foreach (var child in children)
            {
                if (child.DeptId == deptId) return true;
                if (IsDescendantRecursive(child.Children, deptId)) return true;
            }
            return false;
        }

        private static SysDeptVO? FindDeptById(ObservableCollection<SysDeptVO> nodes, long deptId)
        {
            foreach (var node in nodes)
            {
                if (node.DeptId == deptId)
                    return node;

                var found = FindDeptById(node.Children, deptId);
                if (found != null)
                    return found;
            }
            return null;
        }

        partial void OnSelectedParentDeptChanged(SysDeptVO? value)
        {
            OnPropertyChanged(nameof(SelectedParentDeptName));
        }

        /// <summary>
        /// 转换为实体
        /// </summary>
        public SysDept ToEntity()
        {
            return new SysDept
            {
                DeptId = IsEditMode && EditingDept != null ? EditingDept.DeptId : 0,
                ParentId = SelectedParentDept?.DeptId,
                DeptName = DeptName,
                OrderNum = OrderNum,
                Leader = string.IsNullOrWhiteSpace(Leader) ? null : Leader,
                Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone,
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email,
                Status = Status,
                DelFlag = "0"
            };
        }

        public async Task SaveAsync()
        {
            try
            {
                var entity = ToEntity();
                var saved = IsEditMode
                    ? await _deptService.UpdateDeptAsync(entity)
                    : await _deptService.InsertDeptAsync(entity);

                if (saved.Code == 200)
                    _dialogService.ShowSuccess(IsEditMode ? "修改部门成功" : "新增部门成功");
                else
                    _dialogService.ShowError(saved.Msg);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError((IsEditMode ? "修改" : "新增") + "部门失败：" + ex.Message);
            }
        }
    }
}
