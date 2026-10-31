using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.Domain.VO
{
    /// <summary>
    /// 通讯设定单身明细（区域配置）视图对象
    /// </summary>
    public partial class CommSettingAreaVO : ObservableObject
    {
        public long Id { get; set; }

        public long SettingId { get; set; }

        /// <summary>区域：AI / AO / DI / DO</summary>
        [ObservableProperty]
        private string _areaType = "AI";

        /// <summary>对应名称</summary>
        [ObservableProperty]
        private string? _name;

        /// <summary>对应位置</summary>
        [ObservableProperty]
        private string? _position;

        /// <summary>对应数量</summary>
        [ObservableProperty]
        private int _quantity;

        /// <summary>排序号</summary>
        [ObservableProperty]
        private int _sort;
    }
}
