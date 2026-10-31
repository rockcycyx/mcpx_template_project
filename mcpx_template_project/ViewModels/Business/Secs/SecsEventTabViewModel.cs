using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.Secs;
using SecsCeidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsCeid;
using SecsReportEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsReport;

namespace mcpx_template_project.ViewModels.Business.Secs
{
    /// <summary>
    /// Report 多选条目：用于 Event 编辑弹窗中勾选成员 Report。
    /// 按勾选顺序自动生成 sort。
    /// </summary>
    public partial class SecsReportSelectItem : ObservableObject
    {
        public long Id { get; }
        public string Display { get; }

        [ObservableProperty] private bool _isSelected;

        [ObservableProperty] private int _order;

        public SecsReportSelectItem(long id, string display, bool isSelected = false)
        {
            Id = id;
            Display = display;
            _isSelected = isSelected;
        }
    }

    /// <summary>
    /// SECS/GEM 收集事件（Event/CEID）Tab 视图模型：secs_ceid 表 + 成员 Report 的增删改查。
    /// 编辑弹窗多选 Report，sort 按勾选顺序编号。
    /// </summary>
    public partial class SecsEventTabViewModel : ObservableObject
    {
        private readonly ISecsCeidService _ceidService;
        private readonly IDialogService _dialogService;

        public ObservableCollection<SecsCeidEntity> Ceids { get; } = new();

        /// <summary>当前实例 ID（由主 VM 设置，作为所有查询/写入的维度）</summary>
        public long InstanceId { get; set; }

        [ObservableProperty] private SecsCeidEntity? _selectedCeid;

        [ObservableProperty] private string _summary = string.Empty;

        // 分页状态
        [ObservableProperty] private int _pageIndex = 1;
        [ObservableProperty] private int _pageSize = 10;
        [ObservableProperty] private int _totalItems;

        public IReadOnlyList<int> PageSizeOptions { get; } = new[] { 10, 20, 30, 50 };

        /// <summary>数据变更通知（供主 VM 重载左侧树）</summary>
        public event Action? DataChanged;

        public SecsEventTabViewModel(
            ISecsCeidService ceidService,
            IDialogService dialogService)
        {
            _ceidService = ceidService;
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
                var paged = await _ceidService.QueryPagedAsync(InstanceId, paging);
                Ceids.Clear();
                foreach (var item in paged.Items)
                    Ceids.Add(item);
                PageIndex = paged.PageIndex;
                PageSize = paged.PageSize;
                TotalItems = paged.TotalCount;
                Summary = $"共 {paged.TotalCount} 条 Event（CEID）";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载 Event 失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task LoadPage(PagingParameters? paging)
        {
            await LoadPageAsync(paging ?? new PagingParameters { PageIndex = PageIndex, PageSize = PageSize });
        }

        [RelayCommand]
        private async Task AddEvent()
        {
            var reports = await LoadReportsAsync();
            var vm = new SecsCeidFieldViewModel(_dialogService, reports)
            {
                InstanceId = InstanceId,
            };
            if (OpenFieldWindow(vm) != true) return;

            try
            {
                var (entity, reportIds) = vm.ToResult();
                await _ceidService.InsertAsync(entity, reportIds);
                _dialogService.ShowSuccess("新增 Event 成功");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("新增 Event 失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task EditEvent(SecsCeidEntity? ceid)
        {
            if (ceid == null) return;

            var reports = await LoadReportsAsync();
            var vm = new SecsCeidFieldViewModel(_dialogService, reports, ceid);
            if (OpenFieldWindow(vm) != true) return;

            try
            {
                var (entity, reportIds) = vm.ToResult();
                await _ceidService.UpdateAsync(entity, reportIds);
                _dialogService.ShowSuccess("修改 Event 成功");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("修改 Event 失败：" + ex.Message);
            }
        }

        /// <summary>切换启用状态：0(启用)→1(停用)，1(停用)→0(启用)。</summary>
        [RelayCommand]
        private async Task ToggleStatus(SecsCeidEntity? ceid)
        {
            if (ceid == null) return;
            var newStatus = ceid.Status == "1" ? "0" : "1";
            try
            {
                await _ceidService.UpdateStatusAsync(ceid.Id, newStatus);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("切换状态失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task DeleteEvent(SecsCeidEntity? ceid)
        {
            if (ceid == null) return;

            try
            {
                if (!_dialogService.Confirm($"确定删除 Event \"{ceid.Name}\" 吗？", "确认删除"))
                    return;

                await _ceidService.DeleteAsync(ceid.Id);
                _dialogService.ShowSuccess("删除 Event 成功");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("删除 Event 失败：" + ex.Message);
            }
        }

        private async Task<List<SecsReportEntity>> LoadReportsAsync()
        {
            try
            {
                return await _ceidService.QueryReportsAsync(InstanceId);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载 Report 失败：" + ex.Message);
                return new List<SecsReportEntity>();
            }
        }

        private async Task ReloadAsync()
        {
            await LoadAsync();
            DataChanged?.Invoke();
        }

        private bool? OpenFieldWindow(SecsCeidFieldViewModel vm)
        {
            var window = new Views.Pages.Business.Secs.SecsCeidFieldWindow(vm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            return window.ShowDialog();
        }
    }

    /// <summary>
    /// 单个 Event（CEID）字段编辑视图模型（新增/编辑弹窗，含成员 Report 多选）。
    /// Report 按勾选顺序自动生成 sort。
    /// </summary>
    public partial class SecsCeidFieldViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly SecsCeidEntity? _editing;

        /// <summary>所属实例 ID（新增时由 Tab VM 注入；编辑时从实体带出）</summary>
        public long InstanceId { get; set; }

        [ObservableProperty] private string _ceidText = string.Empty;
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _status = "0";
        [ObservableProperty] private string _errorMessage = string.Empty;

        public ObservableCollection<SecsReportSelectItem> ReportOptions { get; } = new();

        public bool IsEditMode => _editing != null;

        public event Action<bool>? RequestClose;

        public SecsCeidFieldViewModel(
            IDialogService dialogService,
            List<SecsReportEntity> reports,
            SecsCeidEntity? editing = null)
        {
            _dialogService = dialogService;
            _editing = editing;

            var selectedIds = editing?.Reports?.Select(r => r.ReportId).ToHashSet() ?? new HashSet<long>();

            foreach (var report in reports)
            {
                ReportOptions.Add(new SecsReportSelectItem(
                    report.Id,
                    $"{report.Name}  (RPTID {report.ReportId})",
                    selectedIds.Contains(report.Id)));
            }

            if (editing != null)
            {
                InstanceId = editing.InstanceId;
                CeidText = editing.Ceid.ToString();
                Name = editing.Name;
                Status = editing.Status;
            }

            RecalculateOrders();
        }

        /// <summary>由 View 在勾选变化后调用，重算选中顺序。</summary>
        public void RecalculateOrders()
        {
            int order = 0;
            foreach (var item in ReportOptions)
            {
                item.Order = item.IsSelected ? ++order : 0;
            }
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (!int.TryParse(CeidText.Trim(), out var ceid) || ceid <= 0 || ceid > 65535)
            {
                ErrorMessage = "CEID 编号必须是 1~65535 的整数";
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

        public (SecsCeidEntity, List<long>) ToResult()
        {
            var entity = new SecsCeidEntity
            {
                Id = _editing?.Id ?? 0,
                InstanceId = InstanceId,
                Ceid = int.Parse(CeidText.Trim()),
                Name = Name.Trim(),
                Status = Status,
            };

            var reportIds = ReportOptions
                .Where(r => r.IsSelected)
                .OrderBy(r => r.Order)
                .Select(r => r.Id)
                .ToList();

            return (entity, reportIds);
        }
    }
}
