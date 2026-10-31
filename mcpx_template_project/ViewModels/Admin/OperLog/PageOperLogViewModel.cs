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

namespace mcpx_template_project.ViewModels.Admin.OperLog
{
    public partial class PageOperLogViewModel : PagedViewModelBase<SysOperLogVO, SysOperLogQuery>
    {
        private readonly IOperLogService _operLogService;

        public ObservableCollection<SysOperLogVO> OperLogList => Items;

        [ObservableProperty]
        private string _searchTitle = string.Empty;

        [ObservableProperty]
        private int? _searchBusinessType;

        [ObservableProperty]
        private string _searchOperName = string.Empty;

        [ObservableProperty]
        private string _searchOperIp = string.Empty;

        [ObservableProperty]
        private int? _searchStatus;

        [ObservableProperty]
        private DateTime? _searchBeginTime;

        [ObservableProperty]
        private DateTime? _searchEndTime;

        [ObservableProperty]
        private SysOperLogVO? _selectedLog;

        [ObservableProperty]
        private bool _isAllSelected;

        protected override string LoadErrorMessage => "加载操作日志失败";

        public PageOperLogViewModel(IOperLogService operLogService, IDialogService dialogService)
            : base(dialogService)
        {
            _operLogService = operLogService;
            _ = RefreshAsync();
        }

        private bool _isSyncingSelectAll;

        private void UpdateSelectAllState()
        {
            _isSyncingSelectAll = true;
            IsAllSelected = OperLogList.Count > 0 && OperLogList.All(p => p.IsSelected);
            _isSyncingSelectAll = false;
        }

        protected override SysOperLogQuery BuildQuery() => new()
        {
            Title = SearchTitle,
            BusinessType = SearchBusinessType,
            OperName = SearchOperName,
            OperIp = SearchOperIp,
            Status = SearchStatus,
            BeginTime = SearchBeginTime,
            EndTime = SearchEndTime
        };

        protected override Task<PagedResult<SysOperLogVO>> QueryPagedAsync(SysOperLogQuery query, PagingParameters paging)
            => _operLogService.QueryOperLogsPagedAsync(query, paging);

        protected override void ResetSearchCore()
        {
            SearchTitle = string.Empty;
            SearchBusinessType = null;
            SearchOperName = string.Empty;
            SearchOperIp = string.Empty;
            SearchStatus = null;
            SearchBeginTime = null;
            SearchEndTime = null;
        }

        protected override void AttachItem(SysOperLogVO item) => item.PropertyChanged += Log_PropertyChanged;

        protected override void DetachItem(SysOperLogVO item) => item.PropertyChanged -= Log_PropertyChanged;

        protected override void OnItemsLoaded() => UpdateSelectAllState();

        [RelayCommand]
        private void SelectAll()
        {
            bool targetState = OperLogList.Count > 0 && !OperLogList.All(p => p.IsSelected);
            _isSyncingSelectAll = true;
            foreach (var log in OperLogList)
            {
                log.IsSelected = targetState;
            }
            _isSyncingSelectAll = false;
            UpdateSelectAllState();
        }

        [RelayCommand]
        private void ViewDetail(SysOperLogVO? log)
        {
            if (log == null) return;
            DialogService.ShowDialog(log);
        }

        [RelayCommand]
        private async Task DeleteLog(SysOperLogVO? log)
        {
            if (log == null) return;

            try
            {
                if (!DialogService.Confirm("确定删除该条操作日志吗？", "确认删除"))
                    return;

                await _operLogService.DeleteOperLogAsync(log.OperId);
                DialogService.ShowSuccess("删除成功");

                if (OperLogList.Count == 1 && PageIndex > 1)
                {
                    PageIndex--;
                }
                _ = RefreshAsync();
            }
            catch (Exception ex)
            {
                DialogService.ShowError("删除失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task BatchDelete()
        {
            try
            {
                var selectedLogs = OperLogList.Where(p => p.IsSelected).ToList();
                if (selectedLogs.Count == 0)
                {
                    DialogService.ShowWarning("请先选择要删除的日志");
                    return;
                }

                if (!DialogService.Confirm($"确定删除选中的 {selectedLogs.Count} 条日志吗？", "确认批量删除"))
                    return;

                foreach (var log in selectedLogs)
                {
                    await _operLogService.DeleteOperLogAsync(log.OperId);
                }
                DialogService.ShowSuccess("批量删除成功");
                _ = RefreshAsync();
            }
            catch (Exception ex)
            {
                DialogService.ShowError("批量删除失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task ClearAll()
        {
            try
            {
                if (!DialogService.Confirm("确定清空所有操作日志吗？此操作不可恢复！", "确认清空"))
                    return;

                await _operLogService.ClearOperLogsAsync();
                DialogService.ShowSuccess("清空成功");
                _ = RefreshAsync();
            }
            catch (Exception ex)
            {
                DialogService.ShowError("清空失败：" + ex.Message);
            }
        }

        partial void OnSelectedLogChanged(SysOperLogVO? value)
        {
        }

        private void Log_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SysOperLogVO.IsSelected) && !_isSyncingSelectAll)
            {
                UpdateSelectAllState();
            }
        }
    }
}
