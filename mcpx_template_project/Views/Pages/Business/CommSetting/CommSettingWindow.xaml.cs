using System.ComponentModel;
using System.Windows;
using mcpx_template_project.Domain.VO;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.ViewModels.Business;

namespace mcpx_template_project.Views.Pages.Business
{
    /// <summary>
    /// 通讯设定列表窗口。
    /// 生命周期不随窗口关闭而消亡：点击关闭按钮仅隐藏窗口，下次从主页面 IOT 图标再次打开时复用同一实例。
    /// </summary>
    public partial class CommSettingWindow : Window
    {
        private readonly CommSettingListViewModel _viewModel;
        private readonly IViewModelFactory _viewModelFactory;

        public CommSettingWindow(CommSettingListViewModel viewModel, IViewModelFactory viewModelFactory)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _viewModelFactory = viewModelFactory;
            DataContext = viewModel;

            _viewModel.RequestEdit += OnRequestEdit;
        }

        private async void OnRequestEdit(CommSettingVO? setting)
        {
            var editVm = setting == null
                ? _viewModelFactory.Create<CommSettingEditViewModel>()
                : _viewModelFactory.Create<CommSettingEditViewModel>(setting);

            var window = new CommSettingEditWindow(editVm) { Owner = this };

            if (window.ShowDialog() == true)
            {
                var saved = await editVm.SaveAsync();
                await _viewModel.LoadAsync(saved?.Id);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // 关闭按钮仅隐藏窗口，保持生命周期，列表数据与状态不丢失
            e.Cancel = true;
            Hide();
            base.OnClosing(e);
        }
    }
}
