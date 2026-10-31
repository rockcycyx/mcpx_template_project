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

namespace mcpx_template_project.ViewModels.Admin.User
{
    public partial class PageUserViewModel : PagedViewModelBase<SysUserVO, SysUserQuery>
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly IDeptService _deptService;
        private readonly IPostService _postService;
        private readonly IViewModelFactory _viewModelFactory;

        public ObservableCollection<SysUserVO> UserList => Items;

        [ObservableProperty]
        private string _searchUserName = string.Empty;

        [ObservableProperty]
        private string _searchPhoneNumber = string.Empty;

        [ObservableProperty]
        private string _searchStatus = string.Empty;

        [ObservableProperty]
        private DateTime? _searchBeginTime;

        [ObservableProperty]
        private DateTime? _searchEndTime;

        [ObservableProperty]
        private SysUserVO? _selectedUser;

        [ObservableProperty]
        private bool _isAllSelected;

        protected override string LoadErrorMessage => "加载用户失败";

        public PageUserViewModel(IUserService userService, IRoleService roleService, IDeptService deptService, IPostService postService, IDialogService dialogService, IViewModelFactory viewModelFactory)
            : base(dialogService)
        {
            _userService = userService;
            _roleService = roleService;
            _deptService = deptService;
            _postService = postService;
            _viewModelFactory = viewModelFactory;

            _ = RefreshAsync();
        }

        private bool _isSyncingSelectAll;

        private void UpdateSelectAllState()
        {
            _isSyncingSelectAll = true;
            IsAllSelected = UserList.Count > 0 && UserList.All(u => u.IsSelected);
            _isSyncingSelectAll = false;
        }

        protected override SysUserQuery BuildQuery() => new()
        {
            UserName = SearchUserName,
            PhoneNumber = SearchPhoneNumber,
            Status = SearchStatus,
            BeginTime = SearchBeginTime,
            EndTime = SearchEndTime
        };

        protected override Task<PagedResult<SysUserVO>> QueryPagedAsync(SysUserQuery query, PagingParameters paging)
            => _userService.QueryUsersPagedAsync(query, paging);

        protected override void ResetSearchCore()
        {
            SearchUserName = string.Empty;
            SearchPhoneNumber = string.Empty;
            SearchStatus = string.Empty;
            SearchBeginTime = null;
            SearchEndTime = null;
        }

        protected override void AttachItem(SysUserVO item) => item.PropertyChanged += User_PropertyChanged;

        protected override void DetachItem(SysUserVO item) => item.PropertyChanged -= User_PropertyChanged;

        protected override void OnItemsLoaded() => UpdateSelectAllState();

        /// <summary>
        /// 用户编辑
        /// </summary>
        [RelayCommand]
        private async Task EditUser(SysUserVO? user)
        {
            if (user == null) return;
            if (user.IsAdmin) return;

            var editUserVm = _viewModelFactory.Create<EditUserViewModel>(user);

            var allRoles = await _roleService.QueryAllActiveRolesAsync();
            foreach (var role in allRoles)
            {
                editUserVm.RoleList.Add(role);
            }

            var userRoleIds = await _roleService.QueryRoleIdsByUserIdAsync(user.UserId);
            foreach (var role in editUserVm.RoleList)
            {
                if (userRoleIds.Contains(role.RoleId))
                {
                    role.IsSelected = true;
                }
            }
            editUserVm.NotifyRolesLoaded();

            var deptTree = await _deptService.QueryDeptTreeAsync(new SysDeptQuery());
            foreach (var dept in deptTree)
            {
                editUserVm.DeptTree.Add(dept);
            }
            editUserVm.NotifyDeptTreeLoaded();

            var allPosts = await _postService.QueryAllPostsAsync();
            foreach (var post in allPosts)
            {
                editUserVm.PostList.Add(post);
            }

            var userPostIds = await _postService.QueryPostIdsByUserIdAsync(user.UserId);
            foreach (var post in editUserVm.PostList)
            {
                if (userPostIds.Contains(post.PostId))
                {
                    post.IsSelected = true;
                }
            }
            editUserVm.NotifyPostsLoaded();

            var result = DialogService.ShowDialog(editUserVm);
            if (result == true)
            {
                await editUserVm.SaveAsync();
                _ = RefreshAsync();
            }
        }

        /// <summary>
        /// 删除用户（单条）
        /// </summary>
        [RelayCommand]
        private void DeleteUser(SysUserVO? user)
        {
            if (user == null) return;
            if (user.IsAdmin) return;

            if (DialogService.Confirm($"确定删除用户「{user.UserName}」吗？"))
            {
                _ = DeleteUsersCoreAsync(new List<SysUserVO> { user });
            }
        }

        /// <summary>
        /// 批量删除选中用户
        /// </summary>
        [RelayCommand]
        private async Task BatchDelete()
        {
            var selectedUsers = UserList.Where(u => u.IsSelected).ToList();
            if (selectedUsers.Count == 0)
            {
                DialogService.ShowWarning("请先勾选要删除的用户");
                return;
            }

            // 过滤掉 admin 用户，防止被批量删除
            var adminUsers = selectedUsers.Where(u => u.IsAdmin).ToList();
            if (adminUsers.Count > 0)
            {
                DialogService.ShowWarning($"admin 用户不能被删除，已自动排除 {adminUsers.Count} 个");
                selectedUsers = selectedUsers.Where(u => !u.IsAdmin).ToList();
                if (selectedUsers.Count == 0) return;
            }

            if (!DialogService.Confirm($"确定删除选中的 {selectedUsers.Count} 个用户吗？"))
            {
                return;
            }

            await DeleteUsersCoreAsync(selectedUsers);
        }

        /// <summary>
        /// 删除核心逻辑（供单条/批量删除复用）
        /// </summary>
        private async Task DeleteUsersCoreAsync(IEnumerable<SysUserVO> users)
        {
            try
            {
                IsLoading = true;
                var userIds = users.Select(u => u.UserId).ToList();
                var result = await _userService.DeleteUsersAsync(userIds);
                if (result.IsSuccess)
                {
                    DialogService.ShowSuccess($"成功删除 {result.Data} 个用户");
                    _ = RefreshAsync();
                }
                else
                {
                    DialogService.ShowError(result.Msg);
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowError("删除用户失败：" + ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 更多操作（如重置密码、分配角色等）
        /// </summary>
        [RelayCommand]
        private void MoreAction(SysUserVO? user)
        {
            if (user == null) return;
            if (user.IsAdmin) return;

            DialogService.ShowInfo($"更多操作：{user.UserName}（功能待实现）");
        }

        [RelayCommand]
        private async Task SwitchStatus(SysUserVO item)
        {
            var newStatus = item.Status == "0" ? "1" : "0";
            await _userService.SwitchStatusAsync(item.UserId, newStatus);
            item.Status = newStatus;
        }

        /// <summary>
        /// 新增用户
        /// </summary>
        [RelayCommand]
        private async Task AddUser()
        {
            var addUserVm = _viewModelFactory.Create<AddUserViewModel>();

            var allRoles = await _roleService.QueryAllActiveRolesAsync();
            foreach (var role in allRoles)
            {
                addUserVm.RoleList.Add(role);
            }

            var deptTree = await _deptService.QueryDeptTreeAsync(new SysDeptQuery());
            foreach (var dept in deptTree)
            {
                addUserVm.DeptTree.Add(dept);
            }

            var allPosts = await _postService.QueryAllPostsAsync();
            foreach (var post in allPosts)
            {
                addUserVm.PostList.Add(post);
            }
            addUserVm.NotifyPostsLoaded();

            var result = DialogService.ShowDialog(addUserVm);
            if (result == true)
            {
                await addUserVm.SaveAsync();
                _ = RefreshAsync();
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

        /// <summary>
        /// 全选/取消全选
        /// </summary>
        [RelayCommand]
        private void SelectAll()
        {
            bool targetState = UserList.Count > 0 && !UserList.All(u => u.IsSelected);

            _isSyncingSelectAll = true;
            foreach (var user in UserList)
            {
                if (!user.IsAdmin)
                    user.IsSelected = targetState;
            }
            _isSyncingSelectAll = false;
            UpdateSelectAllState();
        }

        partial void OnSelectedUserChanged(SysUserVO? value)
        {
        }

        private void User_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SysUserVO.IsSelected) && !_isSyncingSelectAll)
            {
                UpdateSelectAllState();
            }
        }
    }
}
