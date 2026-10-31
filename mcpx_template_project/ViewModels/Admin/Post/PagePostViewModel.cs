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

namespace mcpx_template_project.ViewModels.Admin.Post
{
    public partial class PagePostViewModel : PagedViewModelBase<SysPostVO, SysPostQuery>
    {
        private readonly IPostService _postService;
        private readonly IViewModelFactory _viewModelFactory;

        public ObservableCollection<SysPostVO> PostList => Items;

        [ObservableProperty]
        private string _searchPostCode = string.Empty;

        [ObservableProperty]
        private string _searchPostName = string.Empty;

        [ObservableProperty]
        private string _searchStatus = string.Empty;

        [ObservableProperty]
        private SysPostVO? _selectedPost;

        [ObservableProperty]
        private bool _isAllSelected;

        protected override string LoadErrorMessage => "加载岗位失败";

        public PagePostViewModel(IPostService postService, IDialogService dialogService, IViewModelFactory viewModelFactory)
            : base(dialogService)
        {
            _postService = postService;
            _viewModelFactory = viewModelFactory;
            _ = RefreshAsync();
        }

        private bool _isSyncingSelectAll;

        private void UpdateSelectAllState()
        {
            _isSyncingSelectAll = true;
            IsAllSelected = PostList.Count > 0 && PostList.All(p => p.IsSelected);
            _isSyncingSelectAll = false;
        }

        protected override SysPostQuery BuildQuery() => new()
        {
            PostCode = SearchPostCode,
            PostName = SearchPostName,
            Status = SearchStatus
        };

        protected override Task<PagedResult<SysPostVO>> QueryPagedAsync(SysPostQuery query, PagingParameters paging)
            => _postService.QueryPostsPagedAsync(query, paging);

        protected override void ResetSearchCore()
        {
            SearchPostCode = string.Empty;
            SearchPostName = string.Empty;
            SearchStatus = string.Empty;
        }

        protected override void AttachItem(SysPostVO item) => item.PropertyChanged += Post_PropertyChanged;

        protected override void DetachItem(SysPostVO item) => item.PropertyChanged -= Post_PropertyChanged;

        protected override void OnItemsLoaded() => UpdateSelectAllState();

        [RelayCommand]
        private void SelectAll()
        {
            bool targetState = PostList.Count > 0 && !PostList.All(p => p.IsSelected);
            _isSyncingSelectAll = true;
            foreach (var post in PostList)
            {
                post.IsSelected = targetState;
            }
            _isSyncingSelectAll = false;
            UpdateSelectAllState();
        }

        [RelayCommand]
        private async Task AddPost()
        {
            var vm = _viewModelFactory.Create<AddEditPostViewModel>();
            var result = DialogService.ShowDialog(vm);
            if (result == true)
            {
                await vm.SaveAsync();
                _ = RefreshAsync();
            }
        }

        [RelayCommand]
        private async Task EditPost(SysPostVO? post)
        {
            if (post == null) return;

            var vm = _viewModelFactory.Create<AddEditPostViewModel>(post);
            var result = DialogService.ShowDialog(vm);
            if (result == true)
            {
                await vm.SaveAsync();
                _ = RefreshAsync();
            }
        }

        [RelayCommand]
        private async Task DeletePost(SysPostVO? post)
        {
            if (post == null) return;

            try
            {
                if (!DialogService.Confirm($"确定删除岗位 \"{post.PostName}\" 吗？", "确认删除"))
                    return;

                await _postService.DeletePostAsync(post.PostId);
                DialogService.ShowSuccess("删除岗位成功");

                // 如果当前页只有一条记录，跳到上一页
                if (PostList.Count == 1 && PageIndex > 1)
                {
                    PageIndex--;
                }
                _ = RefreshAsync();
            }
            catch (Exception ex)
            {
                DialogService.ShowError("删除岗位失败：" + ex.Message);
            }
        }

        [RelayCommand]
        private async Task SwitchStatus(SysPostVO item)
        {
            var newStatus = item.Status == "0" ? "1" : "0";
            await _postService.SwitchStatusAsync(item.PostId, newStatus);
            item.Status = newStatus;
        }

        [RelayCommand]
        private async Task BatchDelete()
        {
            try
            {
                var selectedPosts = PostList.Where(p => p.IsSelected).ToList();
                if (selectedPosts.Count == 0)
                {
                    DialogService.ShowWarning("请先选择要删除的岗位");
                    return;
                }

                if (!DialogService.Confirm($"确定删除选中的 {selectedPosts.Count} 个岗位吗？", "确认批量删除"))
                    return;

                foreach (var post in selectedPosts)
                {
                    await _postService.DeletePostAsync(post.PostId);
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
        private void Import()
        {
            DialogService.ShowInfo("导入（功能待实现）");
        }

        [RelayCommand]
        private void Export()
        {
            DialogService.ShowInfo("导出（功能待实现）");
        }

        partial void OnSelectedPostChanged(SysPostVO? value)
        {
        }

        private void Post_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SysPostVO.IsSelected) && !_isSyncingSelectAll)
            {
                UpdateSelectAllState();
            }
        }
    }
}
