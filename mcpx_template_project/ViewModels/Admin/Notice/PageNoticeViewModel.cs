using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.ViewModels.Common;

namespace mcpx_template_project.ViewModels.Admin.Notice
{
    public partial class PageNoticeViewModel : PagedViewModelBase<SysNoticeVO, SysNoticeQuery>
    {
        private readonly INoticeService _noticeService;
        private readonly IViewModelFactory _viewModelFactory;

        public ObservableCollection<SysNoticeVO> NoticeList => Items;

        [ObservableProperty]
        private string _searchNoticeTitle = string.Empty;

        [ObservableProperty]
        private string _searchNoticeType = string.Empty;

        [ObservableProperty]
        private string _searchStatus = string.Empty;

        [ObservableProperty]
        private SysNoticeVO? _selectedNotice;

        [ObservableProperty]
        private bool _isAllSelected;

        protected override string LoadErrorMessage => "加载通知公告失败";

        public PageNoticeViewModel(INoticeService noticeService, IDialogService dialogService, IViewModelFactory viewModelFactory)
            : base(dialogService)
        {
            _noticeService = noticeService;
            _viewModelFactory = viewModelFactory;
            _ = RefreshAsync();
        }

        private bool _isSyncingSelectAll;

        private void UpdateSelectAllState()
        {
            _isSyncingSelectAll = true;
            IsAllSelected = NoticeList.Count > 0 && NoticeList.All(n => n.IsSelected);
            _isSyncingSelectAll = false;
        }

        protected override SysNoticeQuery BuildQuery() => new()
        {
            NoticeTitle = SearchNoticeTitle,
            NoticeType = SearchNoticeType,
            Status = SearchStatus
        };

        protected override Task<PagedResult<SysNoticeVO>> QueryPagedAsync(SysNoticeQuery query, PagingParameters paging)
            => _noticeService.QueryNoticesPagedAsync(query, paging);

        protected override void ResetSearchCore()
        {
            SearchNoticeTitle = string.Empty;
            SearchNoticeType = string.Empty;
            SearchStatus = string.Empty;
        }

        protected override void AttachItem(SysNoticeVO item) => item.PropertyChanged += Notice_PropertyChanged;

        protected override void DetachItem(SysNoticeVO item) => item.PropertyChanged -= Notice_PropertyChanged;

        protected override void OnItemsLoaded() => UpdateSelectAllState();

        [RelayCommand]
        private void SelectAll()
        {
            bool targetState = NoticeList.Count > 0 && !NoticeList.All(n => n.IsSelected);
            _isSyncingSelectAll = true;
            foreach (var notice in NoticeList)
            {
                notice.IsSelected = targetState;
            }
            _isSyncingSelectAll = false;
            UpdateSelectAllState();
        }

        [RelayCommand]
        private async Task AddNotice()
        {
            var vm = _viewModelFactory.Create<AddEditNoticeViewModel>();
            var result = DialogService.ShowDialog(vm);
            if (result == true)
            {
                await vm.SaveAsync();
                WeakReferenceMessenger.Default.Send(new NoticeChangedMessage());
                _ = RefreshAsync();
            }
        }

        [RelayCommand]
        private async Task EditNotice(SysNoticeVO? notice)
        {
            if (notice == null) return;

            var vm = _viewModelFactory.Create<AddEditNoticeViewModel>(notice);
            var result = DialogService.ShowDialog(vm);
            if (result == true)
            {
                await vm.SaveAsync();
                _ = RefreshAsync();
            }
        }

        [RelayCommand]
        private async Task DeleteNotice(SysNoticeVO? notice)
        {
            if (notice == null) return;

            try
            {
                if (!DialogService.Confirm($"确定删除通知 \"{notice.NoticeTitle}\" 吗？", "确认删除"))
                    return;

                await _noticeService.DeleteNoticeAsync(notice.NoticeId);
                DialogService.ShowSuccess("删除通知成功");
                WeakReferenceMessenger.Default.Send(new NoticeChangedMessage());

                if (NoticeList.Count == 1 && PageIndex > 1)
                {
                    PageIndex--;
                }
                _ = RefreshAsync();
            }
            catch (Exception ex)
            {
                DialogService.ShowError("删除通知失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task BatchDelete()
        {
            try
            {
                var selected = NoticeList.Where(n => n.IsSelected).ToList();
                if (selected.Count == 0)
                {
                    DialogService.ShowWarning("请先选择要删除的通知");
                    return;
                }

                if (!DialogService.Confirm($"确定删除选中的 {selected.Count} 条通知吗？", "确认批量删除"))
                    return;

                foreach (var notice in selected)
                {
                    await _noticeService.DeleteNoticeAsync(notice.NoticeId);
                }
                DialogService.ShowSuccess("批量删除成功");
                WeakReferenceMessenger.Default.Send(new NoticeChangedMessage());
                _ = RefreshAsync();
            }
            catch (Exception ex)
            {
                DialogService.ShowError("批量删除失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task SwitchStatus(SysNoticeVO item)
        {
            var newStatus = item.Status == "0" ? "1" : "0";
            await _noticeService.SwitchStatusAsync(item.NoticeId, newStatus);
            item.Status = newStatus;
        }

        /// <summary>查看详情：弹出通知详情对话框，同时标记当前用户已读。</summary>
        [RelayCommand]
        private async Task ViewNotice(SysNoticeVO? notice)
        {
            if (notice == null) return;

            var vm = _viewModelFactory.Create<NoticeDetailViewModel>(notice);
            DialogService.ShowDialog(vm);

            // 查看即标记已读
            if (!notice.IsRead)
            {
                await _noticeService.MarkReadAsync(notice.NoticeId);
                notice.IsRead = true;
                WeakReferenceMessenger.Default.Send(new NoticeChangedMessage());
            }
        }

        [RelayCommand]
        private async Task MarkAllRead()
        {
            try
            {
                await _noticeService.MarkAllReadAsync();
                WeakReferenceMessenger.Default.Send(new NoticeChangedMessage());
                DialogService.ShowSuccess("已全部标记为已读");
                _ = RefreshAsync();
            }
            catch (Exception ex)
            {
                DialogService.ShowError("标记已读失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private void Export()
        {
            DialogService.ShowInfo("导出（功能待实现）");
        }

        private void Notice_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SysNoticeVO.IsSelected) && !_isSyncingSelectAll)
            {
                UpdateSelectAllState();
            }
        }
    }
}
