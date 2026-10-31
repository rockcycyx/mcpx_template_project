using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Domain.Common;
using mcpx_template_project.Domain.Model;
using mcpx_template_project.Service.Admin;

namespace mcpx_template_project.ViewModels.Common
{
    /// <summary>
    /// 分页列表 ViewModel 基类：统一封装分页状态、加载/搜索/重置命令、paging 规范化与结果回写。
    /// 子类只需实现 <see cref="BuildQuery"/>、<see cref="QueryPagedAsync"/>、<see cref="ResetSearchCore"/>，
    /// 并通过具体命名属性（如 UserList）暴露 <see cref="Items"/>。
    /// </summary>
    /// <author>Rudeus</author>
    public abstract partial class PagedViewModelBase<TItem, TQuery> : ObservableObject
        where TItem : class
    {
        protected IDialogService DialogService { get; }

        protected PagedViewModelBase(IDialogService dialogService)
        {
            DialogService = dialogService;
        }

        [ObservableProperty]
        private bool _isLoading;

        // 1-based page index
        [ObservableProperty]
        private int _pageIndex = 1;

        [ObservableProperty]
        private int _pageSize = 10;

        [ObservableProperty]
        private int _totalItems;

        public IEnumerable<int> PageSizeOptions { get; } = new[] { 10, 20, 30, 50 };

        /// <summary>
        /// 当前页数据集合。子类通过具体命名属性暴露。
        /// </summary>
        public ObservableCollection<TItem> Items { get; } = new();

        /// <summary>
        /// 从当前搜索条件构造查询对象。
        /// </summary>
        protected abstract TQuery BuildQuery();

        /// <summary>
        /// 调用 Service 执行分页查询（Count + Skip/Take 在 Service 层完成）。
        /// </summary>
        protected abstract Task<PagedResult<TItem>> QueryPagedAsync(TQuery query, PagingParameters paging);

        /// <summary>
        /// 清空搜索字段，由 <see cref="ResetAsync"/> 调用。
        /// </summary>
        protected abstract void ResetSearchCore();

        /// <summary>
        /// 加载失败时的错误提示前缀，子类可重写。
        /// </summary>
        protected virtual string LoadErrorMessage => "加载数据失败";

        /// <summary>
        /// 旧项从集合移除前调用，子类可重写以取消事件订阅。
        /// </summary>
        protected virtual void DetachItem(TItem item) { }

        /// <summary>
        /// 新项加入集合后调用，子类可重写以订阅事件。
        /// </summary>
        protected virtual void AttachItem(TItem item) { }

        /// <summary>
        /// 数据加载并填充集合完成后调用，子类可重写以执行额外逻辑（如更新全选状态）。
        /// </summary>
        protected virtual void OnItemsLoaded() { }

        /// <summary>
        /// 加载指定页。paging 为 null 时使用当前 PageIndex/PageSize。
        /// </summary>
        [RelayCommand]
        protected virtual async Task LoadPage(PagingParameters? paging)
        {
            try
            {
                IsLoading = true;

                var p = NormalizePaging(paging);
                var paged = await QueryPagedAsync(BuildQuery(), p);

                ApplyPagedResult(paged);
                OnItemsLoaded();
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"{LoadErrorMessage}：{ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 使用当前分页参数刷新当前页（供增删改后调用）。
        /// </summary>
        [RelayCommand]
        protected virtual Task RefreshAsync() => LoadPage(null);

        /// <summary>
        /// 执行搜索：回到第一页并加载。
        /// </summary>
        [RelayCommand]
        protected virtual async Task SearchAsync()
        {
            PageIndex = 1;
            await LoadPage(null);
        }

        /// <summary>
        /// 重置搜索条件并执行搜索。
        /// </summary>
        [RelayCommand]
        protected virtual async Task ResetAsync()
        {
            ResetSearchCore();
            await SearchAsync();
        }

        /// <summary>
        /// 构造分页参数，确保 PageIndex >= 1 且 PageSize > 0。
        /// </summary>
        /// <param name="paging">输入分页参数</param>
        /// <returns>合理分页参数</returns>
        private PagingParameters NormalizePaging(PagingParameters? paging)
        {
            var p = paging ?? new PagingParameters { PageIndex = PageIndex, PageSize = PageSize };
            if (p.PageIndex < 1) p.PageIndex = 1;
            if (p.PageSize <= 0) p.PageSize = 10;
            return p;
        }

        private void ApplyPagedResult(PagedResult<TItem> paged)
        {
            TotalItems = paged.TotalCount;
            PageIndex = paged.PageIndex;
            PageSize = paged.PageSize;

            foreach (var old in Items)
            {
                DetachItem(old);
            }
            Items.Clear();
            foreach (var item in paged.Items)
            {
                AttachItem(item);
                Items.Add(item);
            }
        }
    }
}
