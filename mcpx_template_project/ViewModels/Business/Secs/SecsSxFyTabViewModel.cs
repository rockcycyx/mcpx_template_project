using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Core.SecsRoute;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.Secs;
using SecsSxFyEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsSxFy;

namespace mcpx_template_project.ViewModels.Business.Secs
{
    /// <summary>
    /// SxFy 权限表 Tab 视图模型：secs_sxfy 表的增删改查。
    /// 每条目定义某 (Stream, Function) 在 OffLine / OnLine Local / OnLine Remote 状态下是否允许执行。
    /// 配合方法级 [SecsPermission] 注解做 Controller 前置增强。
    /// Stream/Function 从「已打 [SecsPermission] 注解的路由」下拉选择，而非手动输入。
    /// </summary>
    public partial class SecsSxFyTabViewModel : ObservableObject
    {
        private readonly ISecsSxFyService _sxFyService;
        private readonly IDialogService _dialogService;
        private readonly SecsRouter _router;

        /// <summary>可配置权限的 SxF 候选列表（来自已打 [SecsPermission] 注解的路由）。</summary>
        public IReadOnlyList<SecsSxFyOption> SxFyOptions { get; }

        public ObservableCollection<SecsSxFyEntity> Items { get; } = new();

        /// <summary>当前实例 ID（由主 VM 设置）</summary>
        public long InstanceId { get; set; }

        [ObservableProperty] private SecsSxFyEntity? _selectedItem;

        [ObservableProperty] private string _summary = string.Empty;

        // 查询参数
        [ObservableProperty] private string _filterSxF = string.Empty;
        [ObservableProperty] private bool? _filterOffline;
        [ObservableProperty] private bool? _filterOnlineLocal;
        [ObservableProperty] private bool? _filterOnlineRemote;

        /// <summary>三态筛选选项（null=不限，true=允许，false=禁止）。</summary>
        public IReadOnlyList<SecsSxFyTriStateOption> StateFilterOptions { get; } = new[]
        {
            new SecsSxFyTriStateOption(null, "不限"),
            new SecsSxFyTriStateOption(true, "允许"),
            new SecsSxFyTriStateOption(false, "禁止"),
        };

        // 分页状态
        [ObservableProperty] private int _pageIndex = 1;
        [ObservableProperty] private int _pageSize = 10;
        [ObservableProperty] private int _totalItems;

        public IReadOnlyList<int> PageSizeOptions { get; } = new[] { 10, 20, 30, 50 };

        public event Action? DataChanged;

        public SecsSxFyTabViewModel(
            ISecsSxFyService sxFyService,
            IDialogService dialogService,
            SecsRouter router)
        {
            _sxFyService = sxFyService;
            _dialogService = dialogService;
            _router = router;

            SxFyOptions = _router.Routes
                .Where(r => r.Permission is not null)
                .Select(r => new SecsSxFyOption(r.Stream, r.Function))
                .OrderBy(o => o.Stream).ThenBy(o => o.Function)
                .ToList();
        }

        public async Task LoadAsync()
        {
            await LoadPageAsync(new PagingParameters { PageIndex = PageIndex, PageSize = PageSize });
        }

        private SecsSxFyQuery BuildQuery() => new()
        {
            SxF = FilterSxF,
            Offline = FilterOffline,
            OnlineLocal = FilterOnlineLocal,
            OnlineRemote = FilterOnlineRemote,
        };

        private async Task LoadPageAsync(PagingParameters paging)
        {
            try
            {
                var paged = await _sxFyService.QueryPagedAsync(InstanceId, paging, BuildQuery());
                Items.Clear();
                foreach (var item in paged.Items)
                    Items.Add(item);
                PageIndex = paged.PageIndex;
                PageSize = paged.PageSize;
                TotalItems = paged.TotalCount;
                Summary = $"共 {paged.TotalCount} 条 SxF 权限配置";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载 SxFy 权限表失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task LoadPage(PagingParameters? paging)
        {
            await LoadPageAsync(paging ?? new PagingParameters { PageIndex = PageIndex, PageSize = PageSize });
        }

        /// <summary>应用查询条件（重置到第一页）。</summary>
        [RelayCommand]
        private async Task Search()
        {
            PageIndex = 1;
            await LoadAsync();
        }

        /// <summary>重置查询条件。</summary>
        [RelayCommand]
        private async Task ResetFilter()
        {
            FilterSxF = string.Empty;
            FilterOffline = null;
            FilterOnlineLocal = null;
            FilterOnlineRemote = null;
            PageIndex = 1;
            await LoadAsync();
        }

        /// <summary>点击表格中的 OffLine/OnLine Local/OnLine Remote 复选框后，立即保存该条目的权限。</summary>
        [RelayCommand]
        private async Task SavePermissions(SecsSxFyEntity? item)
        {
            if (item == null) return;
            try
            {
                await _sxFyService.UpdateAsync(new SecsSxFyEntity
                {
                    Id = item.Id,
                    InstanceId = item.InstanceId,
                    Stream = item.Stream,
                    Function = item.Function,
                    Name = item.Name,
                    Status = item.Status,
                    AllowOffline = item.AllowOffline,
                    AllowOnlineLocal = item.AllowOnlineLocal,
                    AllowOnlineRemote = item.AllowOnlineRemote,
                });
                // 静默保存，不弹提示（避免频繁点击打断）；失败才提示
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("保存权限失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task AddSxFy()
        {
            var vm = new SecsSxFyFieldViewModel(_dialogService, SxFyOptions) { InstanceId = InstanceId };
            if (OpenFieldWindow(vm) != true) return;

            try
            {
                await _sxFyService.InsertAsync(vm.ToEntity());
                _dialogService.ShowSuccess("新增 SxFy 权限成功");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("新增 SxFy 权限失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task EditSxFy(SecsSxFyEntity? item)
        {
            if (item == null) return;

            var vm = new SecsSxFyFieldViewModel(_dialogService, SxFyOptions, item);
            if (OpenFieldWindow(vm) != true) return;

            try
            {
                await _sxFyService.UpdateAsync(vm.ToEntity());
                _dialogService.ShowSuccess("修改 SxFy 权限成功");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("修改 SxFy 权限失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task DeleteSxFy(SecsSxFyEntity? item)
        {
            if (item == null) return;

            try
            {
                if (!_dialogService.Confirm($"确定删除 {item.SxF} 权限配置吗？", "确认删除"))
                    return;

                await _sxFyService.DeleteAsync(item.Id);
                _dialogService.ShowSuccess("删除 SxFy 权限成功");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("删除 SxFy 权限失败：" + ex.Message);
            }
        }

        /// <summary>切换启用状态：0(启用)→1(停用)，1(停用)→0(启用)。</summary>
        [RelayCommand]
        private async Task ToggleStatus(SecsSxFyEntity? item)
        {
            if (item == null) return;
            var newStatus = item.Status == "1" ? "0" : "1";
            try
            {
                await _sxFyService.UpdateAsync(new SecsSxFyEntity
                {
                    Id = item.Id,
                    InstanceId = item.InstanceId,
                    Stream = item.Stream,
                    Function = item.Function,
                    Name = item.Name,
                    Status = newStatus,
                    AllowOffline = item.AllowOffline,
                    AllowOnlineLocal = item.AllowOnlineLocal,
                    AllowOnlineRemote = item.AllowOnlineRemote,
                });
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("切换状态失败：" + ex.Message);
            }
        }

        private async Task ReloadAsync()
        {
            await LoadAsync();
            DataChanged?.Invoke();
        }

        private bool? OpenFieldWindow(SecsSxFyFieldViewModel vm)
        {
            var window = new Views.Pages.Business.Secs.SecsSxFyFieldWindow(vm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            return window.ShowDialog();
        }
    }

    /// <summary>
    /// 可配置权限的 SxF 候选项（Stream + Function 组合，来源于已打 [SecsPermission] 注解的路由）。
    /// </summary>
    public sealed class SecsSxFyOption
    {
        public byte Stream { get; }
        public byte Function { get; }

        public SecsSxFyOption(byte stream, byte function)
        {
            Stream = stream;
            Function = function;
        }

        /// <summary>下拉框展示文本，如 "S2F41"。</summary>
        public string SxF => $"S{Stream}F{Function}";
    }

    /// <summary>
    /// 三态筛选选项（用于 OffLine/OnLine Local/OnLine Remote 的「不限/允许/禁止」下拉）。
    /// </summary>
    public sealed class SecsSxFyTriStateOption
    {
        public bool? Value { get; }
        public string Label { get; }

        public SecsSxFyTriStateOption(bool? value, string label)
        {
            Value = value;
            Label = label;
        }
    }

    /// <summary>
    /// 单个 SxFy 权限条目编辑视图模型（新增/编辑弹窗）。
    /// Stream/Function 通过下拉框选择（来自已打 [SecsPermission] 注解的 SxF 候选列表），不支持手动输入。
    /// </summary>
    public partial class SecsSxFyFieldViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly SecsSxFyEntity? _editing;

        public long InstanceId { get; set; }

        /// <summary>下拉候选列表（新增时可选择任意未配置的 SxF）。</summary>
        public IReadOnlyList<SecsSxFyOption> Options { get; }

        /// <summary>当前选中的 SxF 候选。</summary>
        [ObservableProperty] private SecsSxFyOption? _selectedOption;

        /// <summary>是否允许修改 SxF（编辑模式下锁定，仅新增时可选）。</summary>
        public bool CanEditSxF => _editing == null;

        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _status = "0";
        [ObservableProperty] private bool _allowOffline;
        [ObservableProperty] private bool _allowOnlineLocal;
        [ObservableProperty] private bool _allowOnlineRemote;
        [ObservableProperty] private string _errorMessage = string.Empty;

        public bool IsEditMode => _editing != null;

        public event Action<bool>? RequestClose;

        public SecsSxFyFieldViewModel(
            IDialogService dialogService,
            IReadOnlyList<SecsSxFyOption> options,
            SecsSxFyEntity? editing = null)
        {
            _dialogService = dialogService;
            _editing = editing;
            Options = options ?? Array.Empty<SecsSxFyOption>();

            if (editing != null)
            {
                InstanceId = editing.InstanceId;
                SelectedOption = Options.FirstOrDefault(o => o.Stream == editing.Stream && o.Function == editing.Function)
                    ?? new SecsSxFyOption(editing.Stream, editing.Function);
                Name = editing.Name;
                Status = editing.Status;
                AllowOffline = editing.AllowOffline;
                AllowOnlineLocal = editing.AllowOnlineLocal;
                AllowOnlineRemote = editing.AllowOnlineRemote;
            }
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (SelectedOption == null)
            {
                ErrorMessage = "请选择一个 SxF 消息";
                return;
            }

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke(false);

        public SecsSxFyEntity ToEntity()
        {
            var opt = SelectedOption!;
            return new SecsSxFyEntity
            {
                Id = _editing?.Id ?? 0,
                InstanceId = InstanceId,
                Stream = opt.Stream,
                Function = opt.Function,
                Name = Name.Trim(),
                Status = Status,
                AllowOffline = AllowOffline,
                AllowOnlineLocal = AllowOnlineLocal,
                AllowOnlineRemote = AllowOnlineRemote,
            };
        }
    }
}
