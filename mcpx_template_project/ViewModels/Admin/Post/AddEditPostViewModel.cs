using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Entities.System;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.ViewModels.Admin.Post
{
    public partial class AddEditPostViewModel : ObservableObject
    {
        private readonly IPostService _postService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string _postCode = string.Empty;

        [ObservableProperty]
        private string _postName = string.Empty;

        [ObservableProperty]
        private int _postSort;

        [ObservableProperty]
        private string _status = "0";

        [ObservableProperty]
        private string _remark = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private bool _isStatusActive = true;

        partial void OnIsStatusActiveChanged(bool value) => Status = value ? "0" : "1";

        public SysPostVO? EditingPost { get; }

        public event Action<bool>? RequestClose;

        public AddEditPostViewModel(IPostService postService, IDialogService dialogService)
        {
            _postService = postService;
            _dialogService = dialogService;
        }

        public AddEditPostViewModel(SysPostVO editingPost, IPostService postService, IDialogService dialogService)
        {
            _postService = postService;
            _dialogService = dialogService;
            EditingPost = editingPost;
            IsEditMode = true;

            PostCode = editingPost.PostCode;
            PostName = editingPost.PostName;
            PostSort = editingPost.PostSort;
            Status = editingPost.Status ?? "0";
            Remark = editingPost.Remark ?? string.Empty;
            IsStatusActive = editingPost.Status != "1";
        }

        [RelayCommand]
        private void Confirm()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(PostCode))
            {
                ErrorMessage = "岗位编码不能为空";
                return;
            }

            if (string.IsNullOrWhiteSpace(PostName))
            {
                ErrorMessage = "岗位名称不能为空";
                return;
            }

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        /// <summary>
        /// 转换为实体
        /// </summary>
        public SysPost ToEntity()
        {
            return new SysPost
            {
                PostId = IsEditMode && EditingPost != null ? EditingPost.PostId : 0,
                PostCode = PostCode,
                PostName = PostName,
                PostSort = PostSort,
                Status = Status,
                Remark = string.IsNullOrWhiteSpace(Remark) ? null : Remark
            };
        }

        public async Task SaveAsync()
        {
            try
            {
                var entity = ToEntity();
                if (IsEditMode)
                    await _postService.UpdatePostAsync(entity);
                else
                    await _postService.InsertPostAsync(entity);

                _dialogService.ShowSuccess(IsEditMode ? "修改岗位成功" : "新增岗位成功");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError((IsEditMode ? "修改" : "新增") + "岗位失败：" + ex.Message);
            }
        }
    }
}
