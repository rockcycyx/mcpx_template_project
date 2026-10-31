using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Domain.VO.Trigger;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.CommSetting;
using mcpx_template_project.Service.Business.Trigger;
using PlcTriggerEntity = mcpx_template_project.Domain.Entities.Business.Trigger.PlcTrigger;

namespace mcpx_template_project.ViewModels.Business.Trigger
{
    /// <summary>设备代号选项（值 + 显示名）</summary>
    public record TriggerDeviceCodeOption(string Value, string Display);

    /// <summary>警报类别选项（ALCD bit7-1 类别值 + 显示名）</summary>
    public record TriggerAlarmCategoryOption(int Value, string Display);

    /// <summary>
    /// 触发器 IO 设定页视图模型：触发器配置（警报 + 事件）的增删改查 + Excel 导入/导出（仅 admin 可进入）。
    /// 与警报展示页（<see cref="PageAlarmViewModel"/>）分离，独立维护整张 plc_trigger 表。
    /// </summary>
    public partial class PageTriggerViewModel : ObservableObject
    {
        private readonly IPlcTriggerService _triggerService;
        private readonly ICommSettingService _commSettingService;
        private readonly IDialogService _dialogService;
        private readonly PlcEventHandlerRegistry _handlerRegistry;

        public ObservableCollection<PlcTriggerEntity> Triggers { get; } = new();

        /// <summary>警报类别下拉选项（ALCD bit7-1，0~8）。</summary>
        public IReadOnlyList<TriggerAlarmCategoryOption> CategoryOptions { get; } = new[]
        {
            new TriggerAlarmCategoryOption(0, "0 - 未使用"),
            new TriggerAlarmCategoryOption(1, "1 - 人身安全"),
            new TriggerAlarmCategoryOption(2, "2 - 设备安全"),
            new TriggerAlarmCategoryOption(3, "3 - 参数控制警告"),
            new TriggerAlarmCategoryOption(4, "4 - 参数控制错误"),
            new TriggerAlarmCategoryOption(5, "5 - 不可恢复错误"),
            new TriggerAlarmCategoryOption(6, "6 - 设备状态警告"),
            new TriggerAlarmCategoryOption(7, "7 - 注意标志"),
            new TriggerAlarmCategoryOption(8, "8 - 数据完整性"),
        };

        [ObservableProperty] private PlcTriggerEntity? _selectedTrigger;

        // ---- 查询筛选字段 ----

        /// <summary>触发名/警报名（模糊）</summary>
        [ObservableProperty] private string _keyword = string.Empty;

        /// <summary>通讯设定（下拉，null=全部）</summary>
        [ObservableProperty] private long? _filterCommSettingId;

        /// <summary>设备代号（下拉，null=全部）</summary>
        [ObservableProperty] private string? _filterDeviceCode;

        /// <summary>点位地址（模糊）</summary>
        [ObservableProperty] private string _filterAddress = string.Empty;

        /// <summary>类型（下拉，null=全部）</summary>
        [ObservableProperty] private string? _filterTriggerType;

        /// <summary>触发方式（下拉，null=全部）</summary>
        [ObservableProperty] private string? _filterTriggerMode;

        /// <summary>事件处理器（下拉，null=全部）</summary>
        [ObservableProperty] private string? _filterActionKey;

        /// <summary>警报类别（下拉，null=全部）</summary>
        [ObservableProperty] private int? _filterAlarmCategory;

        [ObservableProperty] private string _summary = string.Empty;

        // ---- 筛选下拉选项 ----

        /// <summary>通讯设定下拉选项</summary>
        public ObservableCollection<CommSettingVO> SettingOptions { get; } = new();

        /// <summary>设备代号下拉选项</summary>
        public IReadOnlyList<TriggerDeviceCodeOption> DeviceCodeOptions { get; } = new[]
        {
            new TriggerDeviceCodeOption("D", "D（数据寄存器）"),
            new TriggerDeviceCodeOption("W", "W（链接寄存器）"),
            new TriggerDeviceCodeOption("M", "M（内部继电器）"),
            new TriggerDeviceCodeOption("X", "X（输入继电器）"),
            new TriggerDeviceCodeOption("Y", "Y（输出继电器）"),
            new TriggerDeviceCodeOption("L", "L（锁存继电器）"),
            new TriggerDeviceCodeOption("F", "F（报警器）"),
            new TriggerDeviceCodeOption("B", "B（链接继电器）"),
        };

        /// <summary>事件处理器下拉选项</summary>
        public ObservableCollection<PlcEventHandlerOption> HandlerOptions { get; } = new();

        /// <summary>类型下拉选项（alarm/event）</summary>
        public IReadOnlyList<KeyValuePair<string, string>> TriggerTypeOptions { get; } = new[]
        {
            new KeyValuePair<string, string>("alarm", "警报"),
            new KeyValuePair<string, string>("event", "事件"),
        };

        /// <summary>触发方式下拉选项（on/change）</summary>
        public IReadOnlyList<KeyValuePair<string, string>> TriggerModeOptions { get; } = new[]
        {
            new KeyValuePair<string, string>("on", "ON（上升沿）"),
            new KeyValuePair<string, string>("change", "值变化"),
        };

        public PageTriggerViewModel(
            IPlcTriggerService triggerService,
            ICommSettingService commSettingService,
            IDialogService dialogService,
            PlcEventHandlerRegistry handlerRegistry)
        {
            _triggerService = triggerService;
            _commSettingService = commSettingService;
            _dialogService = dialogService;
            _handlerRegistry = handlerRegistry;
        }

        public async Task InitializeAsync()
        {
            await LoadOptionsAsync();
            await LoadAsync();
        }

        private async Task LoadOptionsAsync()
        {
            try
            {
                var settings = await _commSettingService.QueryAllAsync();
                SettingOptions.Clear();
                foreach (var s in settings)
                    SettingOptions.Add(s);
            }
            catch
            {
                // 忽略：下拉为空不影响查询
            }

            if (HandlerOptions.Count == 0)
            {
                foreach (var h in _handlerRegistry.GetOptions())
                    HandlerOptions.Add(h);
            }
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            try
            {
                var query = new PlcTriggerQuery
                {
                    Keyword = string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim(),
                    CommSettingId = FilterCommSettingId,
                    DeviceCode = string.IsNullOrWhiteSpace(FilterDeviceCode) ? null : FilterDeviceCode.Trim(),
                    Address = string.IsNullOrWhiteSpace(FilterAddress) ? null : FilterAddress.Trim(),
                    TriggerType = string.IsNullOrWhiteSpace(FilterTriggerType) ? null : FilterTriggerType.Trim(),
                    TriggerMode = string.IsNullOrWhiteSpace(FilterTriggerMode) ? null : FilterTriggerMode.Trim(),
                    ActionKey = string.IsNullOrWhiteSpace(FilterActionKey) ? null : FilterActionKey.Trim(),
                    AlarmCategory = FilterAlarmCategory,
                };

                var list = await _triggerService.QueryAllAsync(query);

                // 联表回填通讯设定名称（comm_setting.name），避免表格直接展示 CommSettingId 数字
                var nameMap = SettingOptions.ToDictionary(s => s.Id, s => s.Name);
                foreach (var item in list)
                {
                    item.CommSettingName = nameMap.TryGetValue(item.CommSettingId, out var name)
                        ? name
                        : item.CommSettingId.ToString();
                }

                Triggers.Clear();
                foreach (var item in list)
                    Triggers.Add(item);
                Summary = $"共 {list.Count} 条触发器配置";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载触发器配置失败：" + ex.Message);
            }
        }

        /// <summary>重置查询条件为默认值并重新加载。</summary>
        [RelayCommand]
        private async Task ResetFilter()
        {
            Keyword = string.Empty;
            FilterCommSettingId = null;
            FilterDeviceCode = null;
            FilterAddress = string.Empty;
            FilterTriggerType = null;
            FilterTriggerMode = null;
            FilterActionKey = null;
            FilterAlarmCategory = null;
            await LoadAsync();
        }

        [RelayCommand]
        private async Task AddTrigger()
        {
            var settings = await LoadSettingsAsync();
            var vm = new TriggerFieldViewModel(_dialogService, settings, _handlerRegistry.GetOptions());
            if (OpenFieldWindow(vm) != true) return;

            try
            {
                var entity = vm.ToEntity();
                await _triggerService.InsertAsync(entity);
                _dialogService.ShowSuccess("新增触发器成功");
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("新增触发器失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task EditTrigger(PlcTriggerEntity? trigger)
        {
            if (trigger == null) return;

            var settings = await LoadSettingsAsync();
            var vm = new TriggerFieldViewModel(_dialogService, settings, _handlerRegistry.GetOptions(), trigger);
            if (OpenFieldWindow(vm) != true) return;

            try
            {
                var entity = vm.ToEntity();
                await _triggerService.UpdateAsync(entity);
                _dialogService.ShowSuccess("修改触发器成功");
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("修改触发器失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task DeleteTrigger(PlcTriggerEntity? trigger)
        {
            if (trigger == null) return;

            try
            {
                if (!_dialogService.Confirm($"确定删除触发器 \"{trigger.Name}\" 吗？", "确认删除"))
                    return;

                await _triggerService.DeleteAsync(trigger.Id);
                _dialogService.ShowSuccess("删除触发器成功");
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("删除触发器失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task SwitchStatus(PlcTriggerEntity? trigger)
        {
            if (trigger == null) return;
            var newStatus = trigger.Status == "1" ? "0" : "1";
            try
            {
                await _triggerService.SwitchStatusAsync(trigger.Id, newStatus);
                trigger.Status = newStatus;
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("切换状态失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task UpdateCategory(PlcTriggerEntity? trigger)
        {
            if (trigger == null) return;
            try
            {
                await _triggerService.UpdateCategoryAsync(trigger.Id, trigger.AlarmCategory);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("更新警报类别失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task ImportTrigger()
        {
            var path = PickOpenFile("Excel 文件|*.xlsx;*.xls");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var list = _triggerService.ImportFromExcel(path);
                if (list.Count == 0)
                {
                    _dialogService.ShowWarning("Excel 中无有效数据");
                    return;
                }

                if (!_dialogService.Confirm($"将从 Excel 导入 {list.Count} 条触发器（同点位覆盖），继续吗？", "确认导入"))
                    return;

                var (inserted, updated) = await _triggerService.ImportAsync(list);
                _dialogService.ShowSuccess($"导入完成：新增 {inserted} 条，覆盖 {updated} 条");
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("导入失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private void ExportTrigger()
        {
            var path = PickSaveFile("触发器配置.xlsx");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                _triggerService.Export(path);
                _dialogService.ShowSuccess("导出成功");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("导出失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private void ExportTemplate()
        {
            var path = PickSaveFile("触发器配置模板.xlsx");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                _triggerService.ExportTemplate(path);
                _dialogService.ShowSuccess("模板导出成功");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("模板导出失败：" + ex.Message);
            }
        }

        private async Task<List<CommSettingVO>> LoadSettingsAsync()
        {
            try
            {
                return await _commSettingService.QueryAllAsync();
            }
            catch
            {
                return new List<CommSettingVO>();
            }
        }

        private bool? OpenFieldWindow(TriggerFieldViewModel vm)
        {
            var window = new Views.Pages.Business.Trigger.TriggerFieldWindow(vm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            return window.ShowDialog();
        }

        // ---------- 文件对话框 ----------

        private static string? PickOpenFile(string filter)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = filter,
                CheckFileExists = true
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        private static string? PickSaveFile(string defaultName)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel 文件|*.xlsx",
                FileName = defaultName,
                AddExtension = true
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }
    }

    /// <summary>
    /// 单个触发器字段编辑视图模型（新增/编辑弹窗）。
    /// </summary>
    public partial class TriggerFieldViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly PlcTriggerEntity? _editing;

        [ObservableProperty] private long _commSettingId;
        [ObservableProperty] private string _deviceCode = "M";
        [ObservableProperty] private string _address = string.Empty;
        [ObservableProperty] private string _triggerType = "alarm";
        [ObservableProperty] private string _triggerMode = "on";
        [ObservableProperty] private string? _actionKey;
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _status = "1";
        [ObservableProperty] private string _reportAlarm = "0";
        [ObservableProperty] private string _remark = string.Empty;
        [ObservableProperty] private string _errorMessage = string.Empty;
        [ObservableProperty] private int _alarmCategory;

        public ObservableCollection<CommSettingVO> SettingOptions { get; } = new();

        /// <summary>事件处理器下拉选项（来自注解注册表）</summary>
        public ObservableCollection<PlcEventHandlerOption> HandlerOptions { get; } = new();

        /// <summary>设备代号选项</summary>
        public IReadOnlyList<TriggerDeviceCodeOption> DeviceCodeOptions { get; } = new[]
        {
            new TriggerDeviceCodeOption("D", "D（数据寄存器）"),
            new TriggerDeviceCodeOption("W", "W（链接寄存器）"),
            new TriggerDeviceCodeOption("M", "M（内部继电器）"),
            new TriggerDeviceCodeOption("X", "X（输入继电器）"),
            new TriggerDeviceCodeOption("Y", "Y（输出继电器）"),
            new TriggerDeviceCodeOption("L", "L（锁存继电器）"),
            new TriggerDeviceCodeOption("F", "F（报警器）"),
            new TriggerDeviceCodeOption("B", "B（链接继电器）"),
        };

        public bool IsEditMode => _editing != null;

        /// <summary>是否为事件类型（控制事件处理器下拉框可见性）</summary>
        public bool IsEvent => TriggerType == "event";

        /// <summary>是否为警报类型（控制警报类别下拉框可见性）</summary>
        public bool IsAlarm => TriggerType == "alarm";

        /// <summary>警报类别下拉选项（ALCD bit7-1，0~8）</summary>
        public IReadOnlyList<TriggerAlarmCategoryOption> CategoryOptions { get; } = new[]
        {
            new TriggerAlarmCategoryOption(0, "0 - 未使用"),
            new TriggerAlarmCategoryOption(1, "1 - 人身安全"),
            new TriggerAlarmCategoryOption(2, "2 - 设备安全"),
            new TriggerAlarmCategoryOption(3, "3 - 参数控制警告"),
            new TriggerAlarmCategoryOption(4, "4 - 参数控制错误"),
            new TriggerAlarmCategoryOption(5, "5 - 不可恢复错误"),
            new TriggerAlarmCategoryOption(6, "6 - 设备状态警告"),
            new TriggerAlarmCategoryOption(7, "7 - 注意标志"),
            new TriggerAlarmCategoryOption(8, "8 - 数据完整性"),
        };

        /// <summary>请求关闭窗口（true=确定）</summary>
        public event Action<bool>? RequestClose;

        public TriggerFieldViewModel(
            IDialogService dialogService,
            List<CommSettingVO> settings,
            IReadOnlyList<PlcEventHandlerOption> handlerOptions,
            PlcTriggerEntity? editing = null)
        {
            _dialogService = dialogService;
            _editing = editing;

            foreach (var s in settings)
                SettingOptions.Add(s);

            foreach (var h in handlerOptions)
                HandlerOptions.Add(h);

            if (editing != null)
            {
                CommSettingId = editing.CommSettingId;
                DeviceCode = editing.DeviceCode;
                Address = editing.Address;
                TriggerType = editing.TriggerType;
                TriggerMode = editing.TriggerMode;
                ActionKey = editing.ActionKey;
                AlarmCategory = editing.AlarmCategory;
                Name = editing.Name;
                Status = editing.Status;
                ReportAlarm = editing.ReportAlarm ?? "0";
                Remark = editing.Remark ?? string.Empty;
                OnPropertyChanged(nameof(IsEvent));
                OnPropertyChanged(nameof(IsAlarm));
            }
        }

        partial void OnTriggerTypeChanged(string value)
        {
            OnPropertyChanged(nameof(IsEvent));
            OnPropertyChanged(nameof(IsAlarm));
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (CommSettingId <= 0)
            {
                ErrorMessage = "请选择设备（通讯设定）";
                return;
            }
            if (string.IsNullOrWhiteSpace(DeviceCode))
            {
                ErrorMessage = "请选择类型（设备代号）";
                return;
            }
            if (string.IsNullOrWhiteSpace(Address))
            {
                ErrorMessage = "点位（地址）不能为空";
                return;
            }
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "触发名/警报名不能为空";
                return;
            }
            if (TriggerType == "event" && string.IsNullOrWhiteSpace(ActionKey))
            {
                ErrorMessage = "事件类型必须选择事件处理器";
                return;
            }

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke(false);

        public PlcTriggerEntity ToEntity()
        {
            return new PlcTriggerEntity
            {
                Id = _editing?.Id ?? 0,
                CommSettingId = CommSettingId,
                DeviceCode = DeviceCode,
                Address = Address.Trim(),
                TriggerType = TriggerType,
                TriggerMode = TriggerMode,
                ActionKey = TriggerType == "event" ? ActionKey?.Trim() : null,
                AlarmCategory = TriggerType == "alarm" ? AlarmCategory : 0,
                ReportAlarm = TriggerType == "alarm" ? (ReportAlarm == "1" ? "1" : "0") : "0",
                Name = Name.Trim(),
                Status = Status,
                Remark = string.IsNullOrWhiteSpace(Remark) ? null : Remark.Trim()
            };
        }
    }
}
