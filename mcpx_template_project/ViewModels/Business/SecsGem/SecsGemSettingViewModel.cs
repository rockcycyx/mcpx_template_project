using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Configation;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.CommSetting;
using mcpx_template_project.Service.Business.Secs;
using SecsInstanceEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsInstance;

namespace mcpx_template_project.ViewModels.Business.SecsGem
{
    /// <summary>
    /// SECS/GEM 通讯设定视图模型（多实例，数据表驱动）。
    /// 左侧维护 secs_instance 实例列表（增删改查 + 启停监听），
    /// 右侧编辑当前选中实例的通讯参数（Name/IP/Port/DeviceId/ConnectMode/T3~T8/CommSettingId/Status）。
    /// 生命周期独立于窗体，窗口关闭仅隐藏。
    /// </summary>
    public partial class SecsGemSettingViewModel : ObservableObject
    {
        private readonly ISecsInstanceService _instanceService;
        private readonly ICommSettingService _commSettingService;
        private readonly ISecsGemManager _manager;
        private readonly IDialogService _dialogService;

        // ===== 实例列表 =====
        public ObservableCollection<SecsInstanceEntity> Instances { get; } = new();

        /// <summary>当前选中的实例</summary>
        [ObservableProperty] private SecsInstanceEntity? _selectedInstance;

        /// <summary>PLC 通讯设定下拉选项（关联 comm_setting_id）</summary>
        public ObservableCollection<Domain.VO.CommSettingVO> CommSettings { get; } = new();

        // ===== 编辑区（当前选中实例的字段副本，保存时写回实体） =====
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _ipAddress = string.Empty;
        [ObservableProperty] private string _portText = string.Empty;
        [ObservableProperty] private string _deviceIdText = string.Empty;
        [ObservableProperty] private string _connectMode = "manual";
        [ObservableProperty] private string _t3Text = string.Empty;
        [ObservableProperty] private string _t5Text = string.Empty;
        [ObservableProperty] private string _t6Text = string.Empty;
        [ObservableProperty] private string _t7Text = string.Empty;
        [ObservableProperty] private string _t8Text = string.Empty;
        [ObservableProperty] private Domain.VO.CommSettingVO? _selectedCommSetting;
        [ObservableProperty] private string _status = "0";

        /// <summary>状态胶囊显示文本（0=启用，1=停用）</summary>
        public string StatusText => Status == "0" ? "启用" : "停用";

        // ===== 只读状态 =====
        [ObservableProperty] private string _stateText = "未启动";
        [ObservableProperty] private string _deviceIp = "N/A";
        [ObservableProperty] private bool _isRunning;

        /// <summary>当前编辑值是否已保存（与选中实体一致）</summary>
        [ObservableProperty] private bool _isDirty;

        /// <summary>监听参数是否可编辑（实例监听中时禁用端口输入框与保存按钮）</summary>
        public bool CanEdit => !IsRunning;

        /// <summary>实例是否停用（status=1 时不可启用监听）</summary>
        public bool IsDisabled => Status == "1";

        /// <summary>是否为新增模式（未保存过的新实例）</summary>
        [ObservableProperty] private bool _isNew;

        /// <summary>错误提示文案</summary>
        [ObservableProperty] private string _errorMessage = string.Empty;

        public SecsGemSettingViewModel(
            ISecsInstanceService instanceService,
            ICommSettingService commSettingService,
            ISecsGemManager manager,
            IDialogService dialogService)
        {
            _instanceService = instanceService;
            _commSettingService = commSettingService;
            _manager = manager;
            _dialogService = dialogService;

            _ = LoadAsync();
        }

        /// <summary>当前实例的 deviceKey（通讯层稳定标识）</summary>
        private string DeviceKey => SelectedInstance?.Id.ToString() ?? string.Empty;

        /// <summary>当前实例的通讯引擎（不存在返回 null）</summary>
        private ISecsGemCommunicationService? CurrentEngine =>
            string.IsNullOrEmpty(DeviceKey) ? null : _manager.GetEngine(DeviceKey);

        /// <summary>初始加载：实例列表 + PLC 通讯设定下拉。</summary>
        private async Task LoadAsync()
        {
            await RefreshInstancesAsync(selectFirst: true);
            await RefreshCommSettingsAsync();
        }

        /// <summary>刷新实例列表；selectFirst 为 true 时选中第一项。</summary>
        private async Task RefreshInstancesAsync(bool selectFirst)
        {
            try
            {
                var currentId = SelectedInstance?.Id;
                var instances = await _instanceService.QueryAllAsync();

                Instances.Clear();
                foreach (var inst in instances)
                    Instances.Add(inst);

                if (selectFirst)
                {
                    SelectedInstance = Instances.FirstOrDefault();
                }
                else
                {
                    SelectedInstance = Instances.FirstOrDefault(i => i.Id == currentId)
                                       ?? Instances.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载 SECS 实例失败：" + ex.Message);
            }
        }

        /// <summary>刷新 PLC 通讯设定下拉。</summary>
        private async Task RefreshCommSettingsAsync()
        {
            try
            {
                var settings = await _commSettingService.QueryAllAsync();

                // 记录当前选中实例关联的 comm_setting_id，重建集合后重新匹配，避免下拉框空白
                var currentId = SelectedInstance?.CommSettingId;

                CommSettings.Clear();
                foreach (var s in settings)
                    CommSettings.Add(s);

                // 重建集合后重新绑定选中项，防止 SelectedCommSetting 指向已清除的旧对象导致下拉框空白
                SelectedCommSetting = CommSettings.FirstOrDefault(c => c.Id == currentId);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载 PLC 通讯设定失败：" + ex.Message);
            }
        }

        /// <summary>切换选中实例：回填编辑区并刷新状态。</summary>
        partial void OnSelectedInstanceChanged(SecsInstanceEntity? value)
        {
            if (value == null)
            {
                ClearEditor();
                return;
            }

            Name = value.Name;
            IpAddress = value.IpAddress;
            PortText = value.Port.ToString();
            DeviceIdText = value.DeviceId.ToString();
            ConnectMode = value.ConnectMode;
            T3Text = value.T3.ToString();
            T5Text = value.T5.ToString();
            T6Text = value.T6.ToString();
            T7Text = value.T7.ToString();
            T8Text = value.T8.ToString();
            Status = value.Status;
            SelectedCommSetting = CommSettings.FirstOrDefault(c => c.Id == value.CommSettingId);
            IsNew = false;
            IsDirty = false;
            ErrorMessage = string.Empty;
            RefreshState();
        }

        private void ClearEditor()
        {
            Name = string.Empty;
            IpAddress = string.Empty;
            PortText = string.Empty;
            DeviceIdText = string.Empty;
            ConnectMode = "manual";
            T3Text = string.Empty;
            T5Text = string.Empty;
            T6Text = string.Empty;
            T7Text = string.Empty;
            T8Text = string.Empty;
            Status = "0";
            SelectedCommSetting = null;
            IsDirty = false;
            IsNew = false;
            ErrorMessage = string.Empty;
            StateText = "未启动";
            DeviceIp = "N/A";
            IsRunning = false;
        }

        private void RefreshState()
        {
            var engine = CurrentEngine;
            IsRunning = engine?.IsRunning ?? false;
            StateText = (engine?.IsRunning ?? false) ? engine!.StateText : "未启动";
            DeviceIp = engine?.DeviceIpAddress ?? "N/A";
        }

        partial void OnNameChanged(string value) => MarkDirty();
        partial void OnIpAddressChanged(string value) => MarkDirty();
        partial void OnPortTextChanged(string value) => MarkDirty();
        partial void OnDeviceIdTextChanged(string value) => MarkDirty();
        partial void OnConnectModeChanged(string value) => MarkDirty();
        partial void OnT3TextChanged(string value) => MarkDirty();
        partial void OnT5TextChanged(string value) => MarkDirty();
        partial void OnT6TextChanged(string value) => MarkDirty();
        partial void OnT7TextChanged(string value) => MarkDirty();
        partial void OnT8TextChanged(string value) => MarkDirty();
        partial void OnSelectedCommSettingChanged(Domain.VO.CommSettingVO? value) => MarkDirty();
        partial void OnStatusChanged(string value)
        {
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsDisabled));
            MarkDirty();
        }

        partial void OnIsRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(CanEdit));
        }

        private void MarkDirty()
        {
            if (SelectedInstance != null) IsDirty = true;
        }

        /// <summary>刷新实例列表（保留当前选中项）。</summary>
        [RelayCommand]
        private async Task RefreshInstances()
        {
            await RefreshInstancesAsync(selectFirst: false);
            await RefreshCommSettingsAsync();
        }

        /// <summary>新增：新建一个空白实例进入编辑态。</summary>
        [RelayCommand]
        private void New()
        {
            SelectedInstance = null;
            ClearEditor();
            IsNew = true;
            IsDirty = true;
            Name = string.Empty;
            IpAddress = "0.0.0.0";
            PortText = "5000";
            DeviceIdText = "0";
            ConnectMode = "manual";
            T3Text = "45000";
            T5Text = "10000";
            T6Text = "5000";
            T7Text = "10000";
            T8Text = "5000";
            Status = "0";
            ErrorMessage = string.Empty;
        }

        /// <summary>删除当前选中实例。</summary>
        [RelayCommand]
        private async Task Delete()
        {
            var instance = SelectedInstance;
            if (instance == null)
            {
                _dialogService.ShowWarning("请先选择要删除的实例");
                return;
            }

            if (!_dialogService.Confirm($"确定删除实例「{instance.Name}」？\n关联的 VID/Report/CEID 将一并失去归属。"))
                return;

            try
            {
                // 若正在运行，先停止监听
                if (_manager.IsRunning(instance.Id.ToString()))
                    _manager.Stop(instance.Id.ToString());

                await _instanceService.DeleteAsync(instance.Id);
                await RefreshInstancesAsync(selectFirst: true);
                _dialogService.ShowSuccess("实例已删除");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("删除实例失败：" + ex.Message);
            }
        }

        /// <summary>保存：新增插入 / 修改更新当前编辑内容。</summary>
        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = string.Empty;

            // 字段校验
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "实例名称不能为空";
                return;
            }
            if (string.IsNullOrWhiteSpace(IpAddress))
            {
                ErrorMessage = "IP 地址不能为空";
                return;
            }
            if (!int.TryParse(PortText, out var port) || port <= 0 || port > 65535)
            {
                ErrorMessage = "端口号必须在 1 ~ 65535 之间";
                return;
            }
            if (!ushort.TryParse(DeviceIdText, out var deviceId))
            {
                ErrorMessage = "设备 ID 必须是 0 ~ 65535 之间的整数";
                return;
            }
            int t3, t5, t6, t7, t8;
            if (!int.TryParse(T3Text, out t3)) { ErrorMessage = "T3 必须是整数"; return; }
            if (!int.TryParse(T5Text, out t5)) { ErrorMessage = "T5 必须是整数"; return; }
            if (!int.TryParse(T6Text, out t6)) { ErrorMessage = "T6 必须是整数"; return; }
            if (!int.TryParse(T7Text, out t7)) { ErrorMessage = "T7 必须是整数"; return; }
            if (!int.TryParse(T8Text, out t8)) { ErrorMessage = "T8 必须是整数"; return; }

            try
            {
                if (IsNew)
                {
                    var entity = new SecsInstanceEntity
                    {
                        Name = Name.Trim(),
                        IpAddress = IpAddress.Trim(),
                        Port = port,
                        DeviceId = deviceId,
                        ConnectMode = ConnectMode,
                        T3 = t3,
                        T5 = t5,
                        T6 = t6,
                        T7 = t7,
                        T8 = t8,
                        CommSettingId = SelectedCommSetting?.Id,
                        Status = Status,
                    };
                    var inserted = await _instanceService.InsertAsync(entity);

                    // 刷新列表并选中新实例
                    await RefreshInstancesAsync(selectFirst: false);
                    SelectedInstance = Instances.FirstOrDefault(i => i.Id == inserted.Id);
                    IsDirty = false;
                    IsNew = false;
                    _dialogService.ShowSuccess("实例已新增");
                }
                else
                {
                    var instance = SelectedInstance;
                    if (instance == null)
                    {
                        _dialogService.ShowWarning("请先选择要修改的实例");
                        return;
                    }

                    instance.Name = Name.Trim();
                    instance.IpAddress = IpAddress.Trim();
                    instance.Port = port;
                    instance.DeviceId = deviceId;
                    instance.ConnectMode = ConnectMode;
                    instance.T3 = t3;
                    instance.T5 = t5;
                    instance.T6 = t6;
                    instance.T7 = t7;
                    instance.T8 = t8;
                    instance.CommSettingId = SelectedCommSetting?.Id;
                    instance.Status = Status;

                    var updated = await _instanceService.UpdateAsync(instance);

                    // 用数据库返回实体刷新选中项（避免直接替换破坏列表引用）
                    var idx = Instances.IndexOf(instance);
                    if (idx >= 0) Instances[idx] = updated;
                    SelectedInstance = updated;

                    IsDirty = false;
                    _dialogService.ShowSuccess("实例已保存");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("保存实例失败：" + ex.Message);
            }
        }

        /// <summary>启停当前实例的监听。</summary>
        [RelayCommand]
        private async Task ToggleRun()
        {
            ErrorMessage = string.Empty;

            var instance = SelectedInstance;
            if (instance == null)
            {
                _dialogService.ShowWarning("请先选择实例");
                return;
            }

            var deviceKey = instance.Id.ToString();

            if (_manager.IsRunning(deviceKey))
            {
                _manager.Stop(deviceKey);
                RefreshState();
                return;
            }

            // 停用实例不可启用监听
            if (instance.Status == "1")
            {
                _dialogService.ShowWarning("该实例已停用，无法启用监听。\n请先将状态切换为「启用」后再启动。");
                return;
            }

            // 启动前先保存未保存的编辑
            if (IsDirty)
            {
                await Save();
                if (ErrorMessage.Length > 0) return;
                instance = SelectedInstance!;
                deviceKey = instance.Id.ToString();
            }

            var config = SecsGemConfigStore.ToConfiguration(instance);
            var ok = await _manager.StartAsync(deviceKey, config);
            if (!ok)
            {
                var engine = CurrentEngine;
                _dialogService.ShowError($"SECS/GEM 启动失败：\n\n{engine?.LastError}");
                return;
            }

            RefreshState();
        }

        /// <summary>切换状态胶囊：0=启用 ↔ 1=停用。</summary>
        [RelayCommand]
        private void ToggleStatus()
        {
            Status = Status == "0" ? "1" : "0";
        }
    }
}
