using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Core.Plc;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.Secs;
using SecsVidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsVid;
using SecsVidType = mcpx_template_project.Domain.Entities.Business.Secs.SecsVidType;
using SecsDataType = mcpx_template_project.Domain.Entities.Business.Secs.SecsDataType;

namespace mcpx_template_project.ViewModels.Business.Secs
{
    /// <summary>VID 类别下拉选项（Value 可空表示「全部」过滤）</summary>
    public record SecsVidTypeOption(SecsVidType? Value, string Display);

    /// <summary>值来源下拉选项（db / plc 缓存）</summary>
    public record SecsSourceOption(string Value, string Display);

    /// <summary>数据类型单选项</summary>
    public record SecsDataTypeOption(SecsDataType Value, string Display);

    /// <summary>[PlcRead] 注解字段下拉选项</summary>
    public record SecsFieldOption(int Index, string Display);

    /// <summary>
    /// SECS/GEM VID Tab 视图模型：secs_vid 表的增删改查（分页）。
    /// 新增/编辑时可从 [PlcRead] 注解字段下拉选择，自动带出 PLC 点位信息入库。
    /// </summary>
    public partial class SecsVidTabViewModel : ObservableObject
    {
        private readonly ISecsVidService _vidService;
        private readonly IDialogService _dialogService;
        private readonly PlcFieldCatalog _fieldCatalog;

        public ObservableCollection<SecsVidEntity> Vids { get; } = new();

        /// <summary>当前实例 ID（由主 VM 设置，作为所有查询/写入的维度）</summary>
        public long InstanceId { get; set; }

        [ObservableProperty] private SecsVidEntity? _selectedVid;

        [ObservableProperty] private string _keyword = string.Empty;

        [ObservableProperty] private SecsVidType? _filterType;

        [ObservableProperty] private string _deviceCode = string.Empty;

        [ObservableProperty] private string _plcAddress = string.Empty;

        [ObservableProperty] private string? _filterStatus;

        [ObservableProperty] private string _summary = string.Empty;

        // 分页状态
        [ObservableProperty] private int _pageIndex = 1;
        [ObservableProperty] private int _pageSize = 10;
        [ObservableProperty] private int _totalItems;

        public IReadOnlyList<int> PageSizeOptions { get; } = new[] { 10, 20, 30, 50 };

        public IReadOnlyList<SecsVidTypeOption> TypeOptions { get; } = new[]
        {
            new SecsVidTypeOption(null, "全部"),
            new SecsVidTypeOption(SecsVidType.SVID, "SVID（状态变量）"),
            new SecsVidTypeOption(SecsVidType.DVID, "DVID（数据变量）"),
            new SecsVidTypeOption(SecsVidType.ECID, "ECID（设备常量）"),
        };

        /// <summary>数据变更通知（供主 VM 重载左侧树）</summary>
        public event Action? DataChanged;

        public SecsVidTabViewModel(
            ISecsVidService vidService,
            IDialogService dialogService,
            PlcFieldCatalog fieldCatalog)
        {
            _vidService = vidService;
            _dialogService = dialogService;
            _fieldCatalog = fieldCatalog;
        }

        /// <summary>加载当前页数据。</summary>
        public async Task LoadAsync()
        {
            await LoadPageAsync(new PagingParameters { PageIndex = PageIndex, PageSize = PageSize });
        }

        private async Task LoadPageAsync(PagingParameters paging)
        {
            try
            {
                var paged = await _vidService.QueryPagedAsync(InstanceId, FilterType, Keyword, DeviceCode, PlcAddress, FilterStatus, paging);
                Vids.Clear();
                foreach (var item in paged.Items)
                    Vids.Add(item);
                PageIndex = paged.PageIndex;
                PageSize = paged.PageSize;
                TotalItems = paged.TotalCount;
                Summary = $"共 {paged.TotalCount} 条 VID";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载 VID 失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task LoadPage(PagingParameters? paging)
        {
            await LoadPageAsync(paging ?? new PagingParameters { PageIndex = PageIndex, PageSize = PageSize });
        }

        /// <summary>搜索：回到第一页并加载。</summary>
        [RelayCommand]
        private async Task Search()
        {
            PageIndex = 1;
            await LoadPageAsync(new PagingParameters { PageIndex = 1, PageSize = PageSize });
            DataChanged?.Invoke();
        }

        /// <summary>重置查询参数并重新加载。</summary>
        [RelayCommand]
        private async Task Reset()
        {
            FilterType = null;
            Keyword = string.Empty;
            DeviceCode = string.Empty;
            PlcAddress = string.Empty;
            FilterStatus = null;
            PageIndex = 1;
            await LoadPageAsync(new PagingParameters { PageIndex = 1, PageSize = PageSize });
            DataChanged?.Invoke();
        }

        [RelayCommand]
        private async Task AddVid()
        {
            var vm = new SecsVidFieldViewModel(_dialogService, _fieldCatalog)
            {
                InstanceId = InstanceId,
            };
            if (OpenFieldWindow(vm) != true) return;

            try
            {
                var entity = vm.ToEntity();
                await _vidService.InsertAsync(entity);
                _dialogService.ShowSuccess("新增 VID 成功");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("新增 VID 失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task EditVid(SecsVidEntity? vid)
        {
            if (vid == null) return;

            var vm = new SecsVidFieldViewModel(_dialogService, _fieldCatalog, vid);
            if (OpenFieldWindow(vm) != true) return;

            try
            {
                var entity = vm.ToEntity();
                await _vidService.UpdateAsync(entity);
                _dialogService.ShowSuccess("修改 VID 成功");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("修改 VID 失败：" + ex.Message);
            }
        }

        /// <summary>切换启用状态：0(启用)→1(停用)，1(停用)→0(启用)。</summary>
        [RelayCommand]
        private async Task ToggleStatus(SecsVidEntity? vid)
        {
            if (vid == null) return;
            var newStatus = vid.Status == "1" ? "0" : "1";
            try
            {
                await _vidService.UpdateStatusAsync(vid.Id, newStatus);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("切换状态失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task DeleteVid(SecsVidEntity? vid)
        {
            if (vid == null) return;

            try
            {
                if (!_dialogService.Confirm($"确定删除 VID \"{vid.Name}\" 吗？", "确认删除"))
                    return;

                await _vidService.DeleteAsync(vid.Id);
                _dialogService.ShowSuccess("删除 VID 成功");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("删除 VID 失败：" + ex.Message);
            }
        }

        private async Task ReloadAsync()
        {
            await LoadAsync();
            DataChanged?.Invoke();
        }

        private bool? OpenFieldWindow(SecsVidFieldViewModel vm)
        {
            var window = new Views.Pages.Business.Secs.SecsVidFieldWindow(vm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            return window.ShowDialog();
        }
    }

    /// <summary>
    /// 单个 VID 字段编辑视图模型（新增/编辑弹窗）。
    /// 核心：可从 [PlcRead] 注解字段下拉选择，自动带出设备代号 + PLC 地址。
    /// </summary>
    public partial class SecsVidFieldViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly PlcFieldCatalog _fieldCatalog;
        private readonly SecsVidEntity? _editing;

        /// <summary>所属实例 ID（新增时由 Tab VM 注入；编辑时从实体带出）</summary>
        public long InstanceId { get; set; }

        [ObservableProperty] private SecsVidType _vidType = SecsVidType.SVID;
        [ObservableProperty] private string _vidIdText = string.Empty;
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private SecsDataType _dataType = SecsDataType.A;
        [ObservableProperty] private string _source = "db";
        [ObservableProperty] private string? _plcAddress;
        [ObservableProperty] private string? _deviceCode;
        [ObservableProperty] private string? _defaultValue;
        [ObservableProperty] private string _status = "0";
        [ObservableProperty] private string _errorMessage = string.Empty;

        public IReadOnlyList<SecsVidTypeOption> TypeOptions { get; } = new[]
        {
            new SecsVidTypeOption(SecsVidType.SVID, "SVID（状态变量）"),
            new SecsVidTypeOption(SecsVidType.DVID, "DVID（数据变量）"),
            new SecsVidTypeOption(SecsVidType.ECID, "ECID（设备常量）"),
        };

        /// <summary>数据类型单选项（RadioButton）</summary>
        public IReadOnlyList<SecsDataTypeOption> DataTypeOptions { get; } = new[]
        {
            new SecsDataTypeOption(SecsDataType.A, "A 字符串"),
            new SecsDataTypeOption(SecsDataType.B, "B 二进制"),
            new SecsDataTypeOption(SecsDataType.BOOLEAN, "布尔"),
            new SecsDataTypeOption(SecsDataType.I1, "I1"),
            new SecsDataTypeOption(SecsDataType.I2, "I2"),
            new SecsDataTypeOption(SecsDataType.I4, "I4"),
            new SecsDataTypeOption(SecsDataType.I8, "I8"),
            new SecsDataTypeOption(SecsDataType.U1, "U1"),
            new SecsDataTypeOption(SecsDataType.U2, "U2"),
            new SecsDataTypeOption(SecsDataType.U4, "U4"),
            new SecsDataTypeOption(SecsDataType.U8, "U8"),
            new SecsDataTypeOption(SecsDataType.F4, "F4"),
            new SecsDataTypeOption(SecsDataType.F8, "F8"),
            new SecsDataTypeOption(SecsDataType.L, "L 列表"),
        };

        public IReadOnlyList<SecsSourceOption> SourceOptions { get; } = new[]
        {
            new SecsSourceOption("db", "DB"),
            new SecsSourceOption("plc", "缓存"),
        };

        public ObservableCollection<SecsFieldOption> FieldOptions { get; } = new();

        [ObservableProperty] private int _selectedFieldIndex = -1;

        public bool IsEditMode => _editing != null;

        public bool IsPlcSource => Source == "plc";

        public event Action<bool>? RequestClose;

        public SecsVidFieldViewModel(
            IDialogService dialogService,
            PlcFieldCatalog fieldCatalog,
            SecsVidEntity? editing = null)
        {
            _dialogService = dialogService;
            _fieldCatalog = fieldCatalog;
            _editing = editing;

            BuildFieldOptions();

            if (editing != null)
            {
                InstanceId = editing.InstanceId;
                VidType = editing.VidType;
                VidIdText = editing.VidId.ToString();
                Name = editing.Name;
                DataType = editing.DataType;
                Source = editing.Source;
                PlcAddress = editing.PlcAddress;
                DeviceCode = editing.DeviceCode;
                DefaultValue = editing.DefaultValue;
                Status = editing.Status;
                OnPropertyChanged(nameof(IsPlcSource));
            }
        }

        private void BuildFieldOptions()
        {
            FieldOptions.Clear();
            var fields = _fieldCatalog.Fields;
            for (int i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                FieldOptions.Add(new SecsFieldOption(
                    i,
                    $"{f.OwnerType.Name}.{f.PropertyName}  [{f.FullAddress}]"));
            }
        }

        partial void OnSourceChanged(string value)
        {
            OnPropertyChanged(nameof(IsPlcSource));
        }

        /// <summary>注解字段下拉选择（DropDownButton/Flyout 或 ComboBox 共用入口）。</summary>
        [RelayCommand]
        private void SelectField(SecsFieldOption? option)
        {
            if (option == null) return;
            SelectedFieldIndex = option.Index;
        }

        partial void OnSelectedFieldIndexChanged(int value)
        {
            var field = _fieldCatalog.GetByIndex(value);
            if (field == null) return;

            PlcAddress = field.Address;
            DeviceCode = field.DeviceCode.ToString();
            if (string.IsNullOrWhiteSpace(Name))
                Name = field.PropertyName;

            // 依据反射得到的 CLR 类型自动填充数据类型（安全映射，见 MapClrTypeToDataType）
            DataType = MapClrTypeToDataType(field.DataType);

            if (Source != "plc")
            {
                Source = "plc";
                OnPropertyChanged(nameof(IsPlcSource));
            }
        }

        /// <summary>
        /// 将 [PlcRead] 反射得到的 CLR 类型映射为 SECS 数据类型。
        /// 未知类型安全回退为 A（字符串），避免误判导致落库后解析出错。
        /// </summary>
        private static SecsDataType MapClrTypeToDataType(Type clrType)
        {
            if (clrType == typeof(bool)) return SecsDataType.BOOLEAN;
            if (clrType == typeof(sbyte)) return SecsDataType.I1;
            if (clrType == typeof(short)) return SecsDataType.I2;
            if (clrType == typeof(int)) return SecsDataType.I4;
            if (clrType == typeof(long)) return SecsDataType.I8;
            if (clrType == typeof(byte)) return SecsDataType.U1;
            if (clrType == typeof(ushort)) return SecsDataType.U2;
            if (clrType == typeof(uint)) return SecsDataType.U4;
            if (clrType == typeof(ulong)) return SecsDataType.U8;
            if (clrType == typeof(float)) return SecsDataType.F4;
            if (clrType == typeof(double)) return SecsDataType.F8;
            if (clrType == typeof(string)) return SecsDataType.A;

            // 未识别的类型（枚举等）：不强行猜测，回退为字符串最安全
            return SecsDataType.A;
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (!int.TryParse(VidIdText.Trim(), out var vidId) || vidId <= 0 || vidId > 65535)
            {
                ErrorMessage = "VID 编号必须是 1~65535 的整数";
                return;
            }
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "名称不能为空";
                return;
            }
            if (ContainsChinese(Name))
            {
                ErrorMessage = "名称不能包含中文（数据传输可能不支持）";
                return;
            }
            if (Source == "plc" && (string.IsNullOrWhiteSpace(DeviceCode) || string.IsNullOrWhiteSpace(PlcAddress)))
            {
                ErrorMessage = "缓存来源必须填写设备代号与点位地址";
                return;
            }

            // 固定值仅在 DB 来源生效，需与所选数据类型匹配
            if (Source == "db" && !string.IsNullOrWhiteSpace(DefaultValue))
            {
                if (!TryValidateDefaultValue(DefaultValue.Trim(), DataType, out var reason))
                {
                    ErrorMessage = reason;
                    return;
                }
            }

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke(false);

        /// <summary>
        /// 校验「固定值」字符串是否可解析为指定 SECS 数据类型。
        /// 返回 false 时 <paramref name="reason"/> 携带错误提示。
        /// </summary>
        private static bool TryValidateDefaultValue(string text, SecsDataType dataType, out string reason)
        {
            reason = string.Empty;

            switch (dataType)
            {
                case SecsDataType.BOOLEAN:
                    // 接受 true/false（不区分大小写）或 0/1
                    if (text.Equals("true", StringComparison.OrdinalIgnoreCase)
                        || text.Equals("false", StringComparison.OrdinalIgnoreCase)
                        || text == "0" || text == "1")
                        return true;
                    reason = "数据类型为布尔时，固定值只能填 true / false / 0 / 1";
                    return false;

                case SecsDataType.I1:
                    if (sbyte.TryParse(text, out _)) return true;
                    reason = "数据类型为 I1 时，固定值必须是 -128 ~ 127 的整数";
                    return false;

                case SecsDataType.I2:
                    if (short.TryParse(text, out _)) return true;
                    reason = "数据类型为 I2 时，固定值必须是 -32768 ~ 32767 的整数";
                    return false;

                case SecsDataType.I4:
                    if (int.TryParse(text, out _)) return true;
                    reason = "数据类型为 I4 时，固定值必须是 32 位有符号整数";
                    return false;

                case SecsDataType.I8:
                    if (long.TryParse(text, out _)) return true;
                    reason = "数据类型为 I8 时，固定值必须是 64 位有符号整数";
                    return false;

                case SecsDataType.U1:
                    if (byte.TryParse(text, out _)) return true;
                    reason = "数据类型为 U1 时，固定值必须是 0 ~ 255 的整数";
                    return false;

                case SecsDataType.U2:
                    if (ushort.TryParse(text, out _)) return true;
                    reason = "数据类型为 U2 时，固定值必须是 0 ~ 65535 的整数";
                    return false;

                case SecsDataType.U4:
                    if (uint.TryParse(text, out _)) return true;
                    reason = "数据类型为 U4 时，固定值必须是 32 位无符号整数";
                    return false;

                case SecsDataType.U8:
                    if (ulong.TryParse(text, out _)) return true;
                    reason = "数据类型为 U8 时，固定值必须是 64 位无符号整数";
                    return false;

                case SecsDataType.F4:
                case SecsDataType.F8:
                    if (double.TryParse(text, out _)) return true;
                    reason = "数据类型为浮点时，固定值必须是数字";
                    return false;

                // A / B / L：不强制校验（字符串可任意、二进制/列表此处不做严格约束）
                default:
                    return true;
            }
        }

        /// <summary>判断字符串是否包含中文字符（CJK 统一表意文字区段）。</summary>
        private static bool ContainsChinese(string text)
        {
            foreach (var c in text)
            {
                if (c >= '\u4E00' && c <= '\u9FFF') return true;
            }
            return false;
        }

        public SecsVidEntity ToEntity()
        {
            return new SecsVidEntity
            {
                Id = _editing?.Id ?? 0,
                InstanceId = InstanceId,
                VidType = VidType,
                VidId = int.Parse(VidIdText.Trim()),
                Name = Name.Trim(),
                DataType = DataType,
                Source = Source,
                PlcAddress = Source == "plc" ? PlcAddress?.Trim() : null,
                DeviceCode = Source == "plc" ? DeviceCode?.Trim() : null,
                DefaultValue = Source == "db" ? DefaultValue?.Trim() : null,
                Status = Status,
            };
        }
    }
}
