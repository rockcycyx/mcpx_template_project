using System.Windows.Controls;
using mcpx_template_project.Domain.Entities.Business.Trigger;
using mcpx_template_project.ViewModels.Business.Trigger;

namespace mcpx_template_project.Views.Pages.Business.Trigger
{
    /// <summary>
    /// 触发器 IO 设定页：触发器配置（警报 + 事件）的增删改查 + Excel 导入/导出（仅 admin 可进入）。
    /// 与警报展示页（PageAlarm）分离，独立维护整张 plc_trigger 表。
    /// </summary>
    public partial class PageTrigger : Page
    {
        public PageTrigger(PageTriggerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += async (_, _) => await viewModel.InitializeAsync();
        }

        /// <summary>警报类别下拉变更后立即保存到数据库。</summary>
        private void AlarmCategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not PageTriggerViewModel vm) return;
            if (sender is not ComboBox combo) return;
            if (combo.DataContext is not PlcTrigger trigger) return;

            // 仅在用户交互（已加载且有实际选项变化）时保存，避免初始化赋值触发写库
            if (!combo.IsLoaded) return;

            // 直接取下拉当前选中值，避免依赖绑定回写时序
            if (combo.SelectedValue is int category)
                trigger.AlarmCategory = category;

            vm.UpdateCategoryCommand.Execute(trigger);
        }
    }
}
