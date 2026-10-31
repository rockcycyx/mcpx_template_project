using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.Secs;
using SecsReportEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsReport;
using SecsVidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsVid;

namespace mcpx_template_project.ViewModels.Business.Secs
{
    /// <summary>
    /// VID 多选条目：用于 Report 编辑弹窗中勾选成员 VID。
    /// 按勾选顺序自动生成 sort。
    /// </summary>
    public partial class SecsVidSelectItem : ObservableObject
    {
        public long Id { get; }
        public string Display { get; }

        [ObservableProperty] private bool _isSelected;

        [ObservableProperty] private int _order;

        public SecsVidSelectItem(long id, string display, bool isSelected = false)
        {
            Id = id;
            Display = display;
            _isSelected = isSelected;
        }
    }

    /// <summary>
    /// SECS/GEM Report Tab 视图模型：secs_report 表 + 成员 VID 的增删改查。
    /// 编辑弹窗多选 VID，sort 按勾选顺序编号。
    /// </summary>
    public partial class SecsReportTabViewModel : ObservableObject
    {
        private readonly ISecsReportService _reportService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<SecsReportEntity> Reports { get; } = new();

        /// <summary>当前实例 ID（由主 VM 设置，作为所有查询/写入的维度）</summary>
        public long InstanceId { get; set; }

        [ObservableProperty] private SecsReportEntity? _selectedReport;

        [ObservableProperty] private string _summary = string.Empty;

        // 分页状态
        [ObservableProperty] private int _pageIndex = 1;
        [ObservableProperty] private int _pageSize = 10;
        [ObservableProperty] private int _totalItems;

        public IReadOnlyList<int> PageSizeOptions { get; } = new[] { 10, 20, 30, 50 };

        /// <summary>数据变更通知（供主 VM 重载左侧树）</summary>
        public event Action? DataChanged;

        public SecsReportTabViewModel(
            ISecsReportService reportService,
            IDialogService dialogService)
        {
            _reportService = reportService;
            _dialogService = dialogService;
        }

        public async Task LoadAsync()
        {
            await LoadPageAsync(new PagingParameters { PageIndex = PageIndex, PageSize = PageSize });
        }

        private async Task LoadPageAsync(PagingParameters paging)
        {
            try
            {
                var paged = await _reportService.QueryPagedAsync(InstanceId, paging);
                Reports.Clear();
                foreach (var item in paged.Items)
                    Reports.Add(item);
                PageIndex = paged.PageIndex;
                PageSize = paged.PageSize;
                TotalItems = paged.TotalCount;
                Summary = $"共 {paged.TotalCount} 条 Report";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载 Report 失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task LoadPage(PagingParameters? paging)
        {
            await LoadPageAsync(paging ?? new PagingParameters { PageIndex = PageIndex, PageSize = PageSize });
        }

        [RelayCommand]
        private async Task AddReport()
        {
            var vids = await LoadVidsAsync();
            var vm = new SecsReportFieldViewModel(_dialogService, vids)
            {
                InstanceId = InstanceId,
            };
            if (OpenFieldWindow(vm) != true) return;

            try
            {
                var (entity, vidIds) = vm.ToResult();
                await _reportService.InsertAsync(entity, vidIds);
                _dialogService.ShowSuccess("新增 Report 成功");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("新增 Report 失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task EditReport(SecsReportEntity? report)
        {
            if (report == null) return;

            var vids = await LoadVidsAsync();
            var vm = new SecsReportFieldViewModel(_dialogService, vids, report);
            if (OpenFieldWindow(vm) != true) return;

            try
            {
                var (entity, vidIds) = vm.ToResult();
                await _reportService.UpdateAsync(entity, vidIds);
                _dialogService.ShowSuccess("修改 Report 成功");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("修改 Report 失败：" + ex.Message);
            }
        }

        /// <summary>切换启用状态：0(启用)→1(停用)，1(停用)→0(启用)。</summary>
        [RelayCommand]
        private async Task ToggleStatus(SecsReportEntity? report)
        {
            if (report == null) return;
            var newStatus = report.Status == "1" ? "0" : "1";
            try
            {
                await _reportService.UpdateStatusAsync(report.Id, newStatus);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("切换状态失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task DeleteReport(SecsReportEntity? report)
        {
            if (report == null) return;

            try
            {
                if (!_dialogService.Confirm($"确定删除 Report \"{report.Name}\" 吗？", "确认删除"))
                    return;

                await _reportService.DeleteAsync(report.Id);
                _dialogService.ShowSuccess("删除 Report 成功");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("删除 Report 失败：" + ex.Message);
            }
        }

        private async Task<List<SecsVidEntity>> LoadVidsAsync()
        {
            try
            {
                return await _reportService.QueryVidsAsync(InstanceId);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载 VID 失败：" + ex.Message);
                return new List<SecsVidEntity>();
            }
        }

        private async Task ReloadAsync()
        {
            await LoadAsync();
            DataChanged?.Invoke();
        }

        private bool? OpenFieldWindow(SecsReportFieldViewModel vm)
        {
            var window = new Views.Pages.Business.Secs.SecsReportFieldWindow(vm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            return window.ShowDialog();
        }
    }

    /// <summary>
    /// 单个 Report 字段编辑视图模型（新增/编辑弹窗，含成员 VID 多选）。
    /// VID 按勾选顺序自动生成 sort。
    /// </summary>
    public partial class SecsReportFieldViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly SecsReportEntity? _editing;

        /// <summary>所属实例 ID（新增时由 Tab VM 注入；编辑时从实体带出）</summary>
        public long InstanceId { get; set; }

        [ObservableProperty] private string _reportIdText = string.Empty;
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _status = "0";
        [ObservableProperty] private string _errorMessage = string.Empty;

        public ObservableCollection<SecsVidSelectItem> VidOptions { get; } = new();

        public bool IsEditMode => _editing != null;

        public event Action<bool>? RequestClose;

        public SecsReportFieldViewModel(
            IDialogService dialogService,
            List<SecsVidEntity> vids,
            SecsReportEntity? editing = null)
        {
            _dialogService = dialogService;
            _editing = editing;

            var selectedIds = editing?.Vids?.Select(v => v.VidId).ToHashSet() ?? new HashSet<long>();

            foreach (var vid in vids)
            {
                VidOptions.Add(new SecsVidSelectItem(
                    vid.Id,
                    $"{vid.Name}  ({vid.VidType} {vid.VidId})",
                    selectedIds.Contains(vid.Id)));
            }

            if (editing != null)
            {
                InstanceId = editing.InstanceId;
                ReportIdText = editing.ReportId.ToString();
                Name = editing.Name;
                Status = editing.Status;
            }

            RecalculateOrders();
        }

        /// <summary>由 View 在勾选变化后调用，重算选中顺序。</summary>
        public void RecalculateOrders()
        {
            int order = 0;
            foreach (var item in VidOptions)
            {
                item.Order = item.IsSelected ? ++order : 0;
            }
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (!int.TryParse(ReportIdText.Trim(), out var reportId) || reportId <= 0 || reportId > 65535)
            {
                ErrorMessage = "Report 编号必须是 1~65535 的整数";
                return;
            }
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "名称不能为空";
                return;
            }

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke(false);

        public (SecsReportEntity, List<long>) ToResult()
        {
            var entity = new SecsReportEntity
            {
                Id = _editing?.Id ?? 0,
                InstanceId = InstanceId,
                ReportId = int.Parse(ReportIdText.Trim()),
                Name = Name.Trim(),
                Status = Status,
            };

            var vidIds = VidOptions
                .Where(v => v.IsSelected)
                .OrderBy(v => v.Order)
                .Select(v => v.Id)
                .ToList();

            return (entity, vidIds);
        }
    }
}
