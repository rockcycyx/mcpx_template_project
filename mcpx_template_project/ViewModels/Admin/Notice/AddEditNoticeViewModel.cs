using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.ViewModels.Admin.Notice
{
    public partial class AddEditNoticeViewModel : ObservableObject
    {
        private readonly INoticeService _noticeService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _noticeTitle = string.Empty;

        [ObservableProperty]
        private string _noticeType = "0";

        [ObservableProperty]
        private string _noticeContent = string.Empty;

        [ObservableProperty]
        private string _status = "0";

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private bool _isStatusActive = true;

        partial void OnIsStatusActiveChanged(bool value) => Status = value ? "0" : "1";

        public SysNoticeVO? EditingNotice { get; }

        public event Action<bool>? RequestClose;

        public AddEditNoticeViewModel(INoticeService noticeService, IDialogService dialogService)
        {
            _noticeService = noticeService;
            _dialogService = dialogService;
        }

        public AddEditNoticeViewModel(SysNoticeVO editingNotice, INoticeService noticeService, IDialogService dialogService)
        {
            _noticeService = noticeService;
            _dialogService = dialogService;
            EditingNotice = editingNotice;
            IsEditMode = true;

            NoticeTitle = editingNotice.NoticeTitle;
            NoticeType = editingNotice.NoticeType;
            NoticeContent = editingNotice.NoticeContent ?? string.Empty;
            Status = editingNotice.Status ?? "0";
            IsStatusActive = editingNotice.Status != "1";
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(NoticeTitle))
            {
                ErrorMessage = "通知标题不能为空";
                return;
            }

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        public SysNotice ToEntity()
        {
            return new SysNotice
            {
                NoticeId = IsEditMode && EditingNotice != null ? EditingNotice.NoticeId : 0,
                NoticeTitle = NoticeTitle.Trim(),
                NoticeType = NoticeType,
                NoticeContent = string.IsNullOrWhiteSpace(NoticeContent) ? null : NoticeContent.Trim(),
                Status = Status,
                Source = IsEditMode && EditingNotice != null ? EditingNotice.Source : "人工"
            };
        }

        public async Task SaveAsync()
        {
            try
            {
                var entity = ToEntity();
                if (IsEditMode)
                    await _noticeService.UpdateNoticeAsync(entity);
                else
                    await _noticeService.InsertNoticeAsync(entity);

                _dialogService.ShowSuccess(IsEditMode ? "修改通知成功" : "新增通知成功");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError((IsEditMode ? "修改" : "新增") + "通知失败：" + ex.Message);
            }
        }
    }
}
