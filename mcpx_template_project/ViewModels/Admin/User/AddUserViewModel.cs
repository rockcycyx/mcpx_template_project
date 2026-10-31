using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;


namespace mcpx_template_project.ViewModels.Admin.User
{
    public partial class AddUserViewModel : ObservableObject
    {
        private readonly IUserService _userService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _nickName = string.Empty;

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _phoneNumber = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private long? _deptId;

        [ObservableProperty]
        private string _sex = string.Empty;

        [ObservableProperty]
        private string _status = "0";

        [ObservableProperty]
        private bool _isStatusActive = true;

        partial void OnIsStatusActiveChanged(bool value) => Status = value ? "0" : "1";

        [ObservableProperty]
        private long? _postId;

        [ObservableProperty]
        private string _remark = string.Empty;

        [ObservableProperty]
        private bool _isPasswordVisible;

        public ObservableCollection<SysDeptVO> DeptTree { get; } = new();

        public ObservableCollection<SysPostVO> PostList { get; } = new();

        public ObservableCollection<SysRoleVO> RoleList { get; } = new();

        public List<SexOption> SexOptions { get; } = new()
        {
            new SexOption("男", "0"),
            new SexOption("女", "1"),
            new SexOption("未知", "2")
        };

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isRolePopupOpen;

        [ObservableProperty]
        private string _selectedRolesDisplay = "请选择角色";

        [ObservableProperty]
        private bool _isDeptPopupOpen;

        [ObservableProperty]
        private string _selectedDeptName = "请选择部门";

        [ObservableProperty]
        private bool _isPostPopupOpen;

        [ObservableProperty]
        private string _selectedPostsDisplay = "请选择岗位";

        public event Action<bool>? RequestClose;

        public AddUserViewModel(IUserService userService, IDialogService dialogService)
        {
            _userService = userService;
            _dialogService = dialogService;
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(NickName))
            {
                ErrorMessage = "用户昵称不能为空";
                return;
            }
            if (string.IsNullOrWhiteSpace(UserName))
            {
                ErrorMessage = "用户名称不能为空";
                return;
            }
            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "用户密码不能为空";
                return;
            }

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        [RelayCommand]
        private void ToggleRolePopup()
        {
            IsRolePopupOpen = !IsRolePopupOpen;
        }

        [RelayCommand]
        private void CloseRolePopup()
        {
            IsRolePopupOpen = false;
        }

        [RelayCommand]
        private void ToggleDeptPopup()
        {
            IsDeptPopupOpen = !IsDeptPopupOpen;
        }

        [RelayCommand]
        private void SelectDept(SysDeptVO? dept)
        {
            if (dept != null)
            {
                DeptId = dept.DeptId;
                SelectedDeptName = dept.DeptName;
            }
            IsDeptPopupOpen = false;
        }

        [RelayCommand]
        private void ClearDept()
        {
            DeptId = null;
            SelectedDeptName = "请选择部门";
            IsDeptPopupOpen = false;
        }

        [RelayCommand]
        private void TogglePostPopup()
        {
            IsPostPopupOpen = !IsPostPopupOpen;
        }

        [RelayCommand]
        private void SelectAllPosts()
        {
            bool selectAll = PostList.Any(p => !p.IsSelected);
            foreach (var post in PostList)
            {
                post.IsSelected = selectAll;
            }
            UpdateSelectedPostsDisplay();
        }

        [RelayCommand]
        private void RefreshPostDisplay()
        {
            UpdateSelectedPostsDisplay();
        }

        private void UpdateSelectedPostsDisplay()
        {
            var selected = PostList.Where(p => p.IsSelected).Select(p => p.PostName).ToList();
            SelectedPostsDisplay = selected.Count > 0 ? string.Join(", ", selected) : "请选择岗位";
        }

        [RelayCommand]
        private void SelectAllRoles()
        {
            bool selectAll = RoleList.Any(r => !r.IsSelected);
            foreach (var role in RoleList)
            {
                role.IsSelected = selectAll;
            }
            UpdateSelectedRolesDisplay();
        }

        /// <summary>
        /// 当用户勾选/取消勾选角色时，刷新显示文本
        /// </summary>
        [RelayCommand]
        private void RefreshRoleDisplay()
        {
            UpdateSelectedRolesDisplay();
        }

        private void UpdateSelectedRolesDisplay()
        {
            var selected = RoleList.Where(r => r.IsSelected).Select(r => r.RoleName).ToList();
            SelectedRolesDisplay = selected.Count > 0 ? string.Join(", ", selected) : "请选择角色";
        }

        /// <summary>
        /// 角色列表加载完成后，回显已选角色
        /// </summary>
        public void NotifyRolesLoaded()
        {
            UpdateSelectedRolesDisplay();
        }

        /// <summary>
        /// 部门树加载完成后，回显已选部门
        /// </summary>
        public void NotifyDeptTreeLoaded()
        {
            if (DeptId.HasValue)
            {
                var dept = FindDeptById(DeptTree, DeptId.Value);
                if (dept != null)
                {
                    SelectedDeptName = dept.DeptName;
                }
            }
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

        public SysUser ToEntity()
        {
            return new SysUser
            {
                NickName = NickName,
                UserName = UserName,
                Password = Password,
                PhoneNumber = PhoneNumber,
                Email = Email,
                DeptId = DeptId,
                Sex = Sex,
                Status = Status,
                Remark = Remark,
                DelFlag = "0",
                UserType = "00",
                CreateTime = DateTime.Now
            };
        }

        public async Task SaveAsync()
        {
            try
            {
                var entity = ToEntity();
                var roleIds = GetSelectedRoleIds();
                var postIds = GetSelectedPostIds();
                var apiResult = await _userService.InsertUserAsync(entity, roleIds, postIds);
                if (apiResult.IsSuccess)
                    _dialogService.ShowSuccess("新增用户成功");
                else
                    _dialogService.ShowError(apiResult.Msg);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("新增用户失败：" + ex.Message);
            }
        }

        public void NotifyPostsLoaded()
        {
            UpdateSelectedPostsDisplay();
        }

        public List<long> GetSelectedPostIds()
        {
            return PostList.Where(p => p.IsSelected).Select(p => p.PostId).ToList();
        }

        public List<long> GetSelectedRoleIds()
        {
            return RoleList.Where(r => r.IsSelected).Select(r => r.RoleId).ToList();
        }
    }

    public class SexOption
    {
        public string Label { get; }
        public string Value { get; }

        public SexOption(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }
}
