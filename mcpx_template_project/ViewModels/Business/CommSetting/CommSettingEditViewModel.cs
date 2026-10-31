using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Entities.Business.CommSetting;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.CommSetting;

namespace mcpx_template_project.ViewModels.Business
{
    /// <summary>
    /// 通讯设定编辑视图模型（仅单头/基本信息，单身区域在列表窗口内联编辑）
    /// </summary>
    public partial class CommSettingEditViewModel : ObservableObject
    {
        private readonly ICommSettingService _service;
        private readonly IDialogService _dialogService;

        // ============ 单头（基本信息） ============
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _ip = string.Empty;
        [ObservableProperty] private int _port;
        [ObservableProperty] private string _connectMode = "auto"; // manual 手动 / auto 自动
        [ObservableProperty] private int _checkCycleMs = 1000;
        [ObservableProperty] private int _timeoutMs = 3000;
        [ObservableProperty] private int _retryCycleS = 5;
        [ObservableProperty] private string _remark = string.Empty;
        [ObservableProperty] private string _errorMessage = string.Empty;

        public bool IsEditMode { get; }
        public CommSettingVO? Editing { get; }

        public event Action<bool>? RequestClose;

        public CommSettingEditViewModel(ICommSettingService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
            IsEditMode = false;
        }

        public CommSettingEditViewModel(CommSettingVO editing, ICommSettingService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
            IsEditMode = true;
            Editing = editing;

            Name = editing.Name;
            Ip = editing.Ip;
            Port = editing.Port;
            ConnectMode = editing.ConnectMode;
            CheckCycleMs = editing.CheckCycleMs;
            TimeoutMs = editing.TimeoutMs;
            RetryCycleS = editing.RetryCycleS;
            Remark = editing.Remark ?? string.Empty;
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "名称不能为空";
                return;
            }
            if (string.IsNullOrWhiteSpace(Ip))
            {
                ErrorMessage = "IP 地址不能为空";
                return;
            }
            if (Port <= 0 || Port > 65535)
            {
                ErrorMessage = "端口号必须在 1 ~ 65535 之间";
                return;
            }

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke(false);

        public CommSetting ToEntity()
        {
            return new CommSetting
            {
                Id = IsEditMode && Editing != null ? Editing.Id : 0,
                Name = Name?.Trim() ?? string.Empty,
                Ip = Ip?.Trim() ?? string.Empty,
                Port = Port,
                ConnectMode = ConnectMode,
                CheckCycleMs = CheckCycleMs,
                TimeoutMs = TimeoutMs,
                RetryCycleS = RetryCycleS,
                Remark = string.IsNullOrWhiteSpace(Remark) ? null : Remark
            };
        }

        public async Task<CommSettingVO?> SaveAsync()
        {
            try
            {
                var entity = ToEntity();

                var saved = IsEditMode
                    ? await _service.UpdateAsync(entity)
                    : await _service.InsertAsync(entity);

                _dialogService.ShowSuccess(IsEditMode ? "修改通讯设定成功" : "新增通讯设定成功");
                return saved;
            }
            catch (Exception ex)
            {
                _dialogService.ShowError((IsEditMode ? "修改" : "新增") + "通讯设定失败：" + ex.Message);
                return null;
            }
        }
    }
}
