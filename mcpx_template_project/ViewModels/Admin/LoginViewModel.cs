using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using mcpx_template_project.Aspect.Attributes;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.Enums;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Core.Logging;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.ViewModels.Admin
{
    /// <summary>
    /// 登录视图模型
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly IUserSessionService _session;
        private readonly IDialogService _dialogService;

        public ILogService LogService { get; }
        public IOperLogService OperLogService { get; }

        [ObservableProperty]
        private LoginBody _loginBody = new();

        [ObservableProperty]
        private string _tipMessage = string.Empty;

        [ObservableProperty]
        private bool _isLoginLoading = false;
        private bool CanLogin() => !IsLoginLoading;

        public bool IsLoggedIn => _session.IsLoggedIn;

        public SysUser? CurrentUser => _session.CurrentUser;

        public LoginViewModel(
            IUserService userService,
            IAuthService authService,
            ILogService logService,
            IUserSessionService session,
            IOperLogService operLogService,
            IDialogService dialogService)
        {
            _userService = userService;
            _authService = authService;
            LogService = logService;
            _session = session;
            OperLogService = operLogService;
            _dialogService = dialogService;
        }

        // [Log(Title = "用户登录", BusinessType = BusinessType.Grant)]
        [RelayCommand(CanExecute = nameof(CanLogin))]
        public async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(LoginBody.EmployeeId))
            {
                _dialogService.ShowError("账号为空异常");
                return;
            }
            if (string.IsNullOrWhiteSpace(LoginBody.Password))
            {
                _dialogService.ShowError("密码为空");
                return;
            }

            IsLoginLoading = true;
            TipMessage = string.Empty;

            try
            {
                var result = await _userService.LoginAsync(LoginBody.EmployeeId, LoginBody.Password);

                if (result.IsSuccess)
                {
                    var user = result.Data!;

                    var isAdmin = await _authService.IsAdminAsync(user.UserId);
                    var perms = await _authService.GetPermsAsync(user.UserId);
                    var menus = await _authService.GetMenuTreeAsync(user.UserId);

                    _session.Login(user, new HashSet<string>(perms), menus, isAdmin);

                    WeakReferenceMessenger.Default.Send(new LoginSuccessMessage(user));
                }
                else
                {
                    _dialogService.ShowError("账号或密码错误");
                    LoginBody.Password = string.Empty;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message);
            }
            finally
            {
                IsLoginLoading = false;
            }
        }

        [RelayCommand]
        private void Logout()
        {
            _session.Logout();
            WeakReferenceMessenger.Default.Send(new LogoutMessage());
        }
    }
}
