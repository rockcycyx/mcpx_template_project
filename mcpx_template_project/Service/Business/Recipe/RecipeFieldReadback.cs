using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.Service.Business.Recipe
{
    /// <summary>
    /// 配方读回显示项：承载字段中文名、目标值（来自选中配方）与当前读回值（来自 PLC 读点）。
    /// 由展示层（ViewModel）构建，底层只返回「属性名 → 当前值」原始字典。
    /// </summary>
    public partial class RecipeFieldReadback : ObservableObject
    {
        /// <summary>属性名</summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>中文显示名</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>目标值（本次写入希望 PLC 得到的值）</summary>
        public string TargetValue { get; set; } = string.Empty;

        /// <summary>当前读回值（PLC 读点实时值）</summary>
        [ObservableProperty]
        private string _currentValue = string.Empty;

        /// <summary>读回值是否已与目标值一致</summary>
        [ObservableProperty]
        private bool _matched;
    }
}
