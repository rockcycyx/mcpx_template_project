namespace mcpx_template_project.Service.Admin
{
    /// <summary>
    /// 对话框服务抽象，使 ViewModel 不直接依赖 WPF 的 MessageBox 与具体窗口类型。
    /// </summary>
    public interface IDialogService
    {
        /// <summary>提示信息对话框</summary>
        void ShowInfo(string message, string title = "提示");

        /// <summary>成功对话框</summary>
        void ShowSuccess(string message, string title = "成功");

        /// <summary>错误对话框</summary>
        void ShowError(string message, string title = "错误");

        /// <summary>警告对话框</summary>
        void ShowWarning(string message, string title = "警告");

        /// <summary>确认对话框，返回 true 表示用户选择"是"</summary>
        bool Confirm(string message, string title = "确认");

        /// <summary>
        /// 显示 viewModel 对应的模态对话框窗口，返回 true 表示用户点击了确定。
        /// 视图模型类型到窗口的映射统一注册在实现类中，新增对话框无需修改本接口。
        /// </summary>
        bool? ShowDialog<TViewModel>(TViewModel viewModel);
    }
}
