using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.ViewModels.Admin.UserCenter
{
    public partial class PageUserCenterViewModel : ObservableObject
    {
        private readonly IUserService _userService;
        private readonly IUserSessionService _session;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private SysUser? _currentUser;

        [ObservableProperty]
        private string _nickName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phoneNumber = string.Empty;

        [ObservableProperty]
        private string _sex = "0";

        [ObservableProperty]
        private string _deptName = string.Empty;

        [ObservableProperty]
        private string _roleName = string.Empty;

        [ObservableProperty]
        private string _oldPassword = string.Empty;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private string _confirmPassword = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public PageUserCenterViewModel(
            IUserService userService,
            IUserSessionService session,
            IDialogService dialogService)
        {
            _userService = userService;
            _session = session;
            _dialogService = dialogService;
            LoadUserInfo();
        }

        private void LoadUserInfo()
        {
            CurrentUser = _session.CurrentUser;
            if (CurrentUser != null)
            {
                NickName = CurrentUser.NickName;
                Email = CurrentUser.Email ?? string.Empty;
                PhoneNumber = CurrentUser.PhoneNumber ?? string.Empty;
                Sex = CurrentUser.Sex ?? "0";
                DeptName = _session.CurrentUser?.DeptId.HasValue == true ? $"部门ID: {_session.CurrentUser.DeptId}" : "-";
                RoleName = _session.IsAdmin ? "超级管理员" : "普通用户";
            }
        }

        [RelayCommand]
        private async Task SaveProfile()
        {
            if (CurrentUser == null) return;

            if (string.IsNullOrWhiteSpace(NickName))
            {
                _dialogService.ShowError("用户昵称不能为空");
                return;
            }

            IsLoading = true;
            try
            {
                var result = await _userService.UpdateUserProfileAsync(
                    CurrentUser.UserId, NickName, Email, PhoneNumber, Sex);

                if (result.IsSuccess)
                {
                    _dialogService.ShowSuccess("资料修改成功");
                    CurrentUser.NickName = NickName;
                    CurrentUser.Email = Email;
                    CurrentUser.PhoneNumber = PhoneNumber;
                    CurrentUser.Sex = Sex;
                    OnPropertyChanged(nameof(CurrentUser));
                }
                else
                {
                    _dialogService.ShowError(result.Msg);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"保存失败：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ChangePassword()
        {
            if (CurrentUser == null) return;

            if (string.IsNullOrWhiteSpace(OldPassword))
            {
                _dialogService.ShowError("请输入原密码");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                _dialogService.ShowError("请输入新密码");
                return;
            }

            if (NewPassword.Length < 6)
            {
                _dialogService.ShowError("新密码长度不能少于6位");
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                _dialogService.ShowError("两次输入的新密码不一致");
                return;
            }

            IsLoading = true;
            try
            {
                var result = await _userService.ChangePasswordAsync(
                    CurrentUser.UserId, OldPassword, NewPassword);

                if (result.IsSuccess)
                {
                    _dialogService.ShowSuccess("密码修改成功");
                    OldPassword = string.Empty;
                    NewPassword = string.Empty;
                    ConfirmPassword = string.Empty;
                }
                else
                {
                    _dialogService.ShowError(result.Msg);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"密码修改失败：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ResetProfile()
        {
            LoadUserInfo();
        }
    }
}
