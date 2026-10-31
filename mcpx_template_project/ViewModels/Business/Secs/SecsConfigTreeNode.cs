using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace mcpx_template_project.ViewModels.Business.Secs
{
    /// <summary>树节点类别：用于区分 CEID / Report / VID 三类节点。</summary>
    public enum SecsTreeNodeKind
    {
        Ceid,
        Report,
        Vid
    }

    /// <summary>
    /// SECS/GEM 配置树节点（左侧总图）。
    /// 层级：CEID → Report → VID；未挂到任何 Report 的 VID 也可直接展示在根下。
    /// </summary>
    public class SecsConfigTreeNode : ObservableObject
    {
        /// <summary>节点标题</summary>
        public string Title { get; }

        /// <summary>节点副标题（编号/类型信息）</summary>
        public string? Subtitle { get; }

        /// <summary>节点类别</summary>
        public SecsTreeNodeKind Kind { get; }

        /// <summary>是否启用（停用节点置灰）</summary>
        public bool Enabled { get; }

        /// <summary>节点图标文本（按类别显示，如 ▣/▤/•）</summary>
        public string Icon { get; }

        /// <summary>子节点</summary>
        public ObservableCollection<SecsConfigTreeNode> Children { get; } = new();

        public SecsConfigTreeNode(string title, SecsTreeNodeKind kind, string? subtitle = null, bool enabled = true)
        {
            Title = title;
            Kind = kind;
            Subtitle = subtitle;
            Enabled = enabled;
            Icon = kind switch
            {
                SecsTreeNodeKind.Ceid => "⚑",
                SecsTreeNodeKind.Report => "▤",
                _ => "•"
            };
        }
    }
}
