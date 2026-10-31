using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mcpx_template_project.Service.Admin;
using mcpx_template_project.Service.Business.Secs;
using SecsCeidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsCeid;
using SecsReportEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsReport;
using SecsVidEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsVid;
using SecsInstanceEntity = mcpx_template_project.Domain.Entities.Business.Secs.SecsInstance;

namespace mcpx_template_project.ViewModels.Business.Secs
{
    /// <summary>
    /// SECS/GEM 配置总览页视图模型。
    /// 顶部：实例选择器（多实例场景，切换后三个 Tab 与左侧树均按当前实例过滤）。
    /// 左侧：CEID→Report→VID 完整层级树（当前实例）；
    /// 右侧：Tab 分页签，VID / Report / Event 三个 CRUD 分别由独立 Tab VM 承载。
    /// </summary>
    public partial class PageSecsConfigViewModel : ObservableObject
    {
        private readonly ISecsInstanceService _instanceService;
        private readonly ISecsVidService _vidService;
        private readonly ISecsReportService _reportService;
        private readonly ISecsCeidService _ceidService;
        private readonly IDialogService _dialogService;

        /// <summary>三个 Tab 的内容 VM</summary>
        public SecsVidTabViewModel VidTab { get; }
        public SecsReportTabViewModel ReportTab { get; }
        public SecsEventTabViewModel EventTab { get; }

        /// <summary>SxFy 权限表 Tab VM</summary>
        public SecsSxFyTabViewModel SxFyTab { get; }

        /// <summary>HCMD（Host 指令）Tab VM</summary>
        public SecsHcmdTabViewModel HcmdTab { get; }

        /// <summary>实例下拉选项</summary>
        public ObservableCollection<SecsInstanceEntity> Instances { get; } = new();

        /// <summary>当前选中的实例</summary>
        [ObservableProperty] private SecsInstanceEntity? _selectedInstance;

        /// <summary>左侧完整树（CEID→Report→VID）</summary>
        public ObservableCollection<SecsConfigTreeNode> TreeNodes { get; } = new();

        [ObservableProperty] private int _selectedTabIndex;

        /// <summary>左侧「SECS/GEM 结构总图」是否展开</summary>
        [ObservableProperty] private bool _isTreeExpanded = true;

        [RelayCommand]
        private void ToggleTree() => IsTreeExpanded = !IsTreeExpanded;

        public PageSecsConfigViewModel(
            ISecsInstanceService instanceService,
            ISecsVidService vidService,
            ISecsReportService reportService,
            ISecsCeidService ceidService,
            IDialogService dialogService,
            SecsVidTabViewModel vidTab,
            SecsReportTabViewModel reportTab,
            SecsEventTabViewModel eventTab,
            SecsSxFyTabViewModel sxFyTab,
            SecsHcmdTabViewModel hcmdTab)
        {
            _instanceService = instanceService;
            _vidService = vidService;
            _reportService = reportService;
            _ceidService = ceidService;
            _dialogService = dialogService;

            VidTab = vidTab;
            ReportTab = reportTab;
            EventTab = eventTab;
            SxFyTab = sxFyTab;
            HcmdTab = hcmdTab;

            // 三个 Tab 数据变更时重载左侧树
            VidTab.DataChanged += OnDataChanged;
            ReportTab.DataChanged += OnDataChanged;
            EventTab.DataChanged += OnDataChanged;
        }

        public async Task InitializeAsync()
        {
            await LoadInstancesAsync();
        }

        /// <summary>加载实例列表并选中第一个。</summary>
        private async Task LoadInstancesAsync()
        {
            try
            {
                var instances = await _instanceService.QueryAllAsync();
                Instances.Clear();
                foreach (var inst in instances)
                    Instances.Add(inst);

                SelectedInstance = Instances.FirstOrDefault();
                await ReloadAllAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载 SECS 实例失败：" + ex.Message);
            }
        }

        /// <summary>切换实例：更新三个 Tab 的实例维度并重载。</summary>
        partial void OnSelectedInstanceChanged(SecsInstanceEntity? value)
        {
            if (value == null) return;
            _ = ReloadAllAsync();
        }

        /// <summary>重载左侧树 + 三个 Tab 列表。</summary>
        [RelayCommand]
        private async Task ReloadAll()
        {
            await ReloadAllAsync();
        }

        /// <summary>刷新实例列表（保留当前选中项）。</summary>
        [RelayCommand]
        private async Task RefreshInstances()
        {
            try
            {
                var currentId = SelectedInstance?.Id;
                var instances = await _instanceService.QueryAllAsync();
                Instances.Clear();
                foreach (var inst in instances)
                    Instances.Add(inst);

                SelectedInstance = Instances.FirstOrDefault(i => i.Id == currentId)
                                   ?? Instances.FirstOrDefault();
                await ReloadAllAsync();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("刷新 SECS 实例失败：" + ex.Message);
            }
        }

        private async Task ReloadAllAsync()
        {
            var instance = SelectedInstance;
            if (instance == null)
            {
                VidTab.Vids.Clear();
                ReportTab.Reports.Clear();
                EventTab.Ceids.Clear();
                SxFyTab.Items.Clear();
                TreeNodes.Clear();
                return;
            }

            long instanceId = instance.Id;
            VidTab.InstanceId = instanceId;
            ReportTab.InstanceId = instanceId;
            EventTab.InstanceId = instanceId;
            SxFyTab.InstanceId = instanceId;
            HcmdTab.InstanceId = instanceId;

            // 并行加载各 Tab 的数据（各自容错）
            await Task.WhenAll(
                VidTab.LoadAsync(),
                ReportTab.LoadAsync(),
                EventTab.LoadAsync(),
                SxFyTab.LoadAsync(),
                HcmdTab.LoadAsync());

            // 重载左侧树（依赖三类完整数据）
            await RebuildTreeAsync(instanceId);
        }

        private void OnDataChanged()
        {
            var instance = SelectedInstance;
            if (instance == null) return;
            _ = RebuildTreeAsync(instance.Id);
        }

        /// <summary>
        /// 构建左侧完整树：CEID → Report → VID（当前实例）。
        /// 未挂到任何 Report 的 VID 归入「未分配 VID」根节点。
        /// </summary>
        private async Task RebuildTreeAsync(long instanceId)
        {
            try
            {
                var ceids = await _ceidService.QueryAllAsync(instanceId);
                var reports = await _reportService.QueryAllAsync(instanceId);
                var vids = await _vidService.QueryAllAsync(instanceId);

                TreeNodes.Clear();

                // 已挂到某 Report 的 VID 集合（用于找出未分配 VID）
                var assignedVidIds = new HashSet<long>();
                var reportNodes = new Dictionary<long, SecsConfigTreeNode>();

                foreach (var report in reports)
                {
                    var reportNode = new SecsConfigTreeNode(
                        $"RPTID {report.ReportId}：{report.Name}",
                        SecsTreeNodeKind.Report,
                        report.Status == "0" ? "启用" : "停用",
                        report.Status == "0");

                    foreach (var rv in report.Vids.OrderBy(v => v.Sort))
                    {
                        var vid = rv.Vid;
                        if (vid == null) continue;
                        assignedVidIds.Add(vid.Id);
                        reportNode.Children.Add(BuildVidNode(vid));
                    }

                    reportNodes[report.Id] = reportNode;
                }

                foreach (var ceid in ceids)
                {
                    var ceidNode = new SecsConfigTreeNode(
                        $"CEID {ceid.Ceid}：{ceid.Name}",
                        SecsTreeNodeKind.Ceid,
                        ceid.Status == "0" ? "启用" : "停用",
                        ceid.Status == "0");

                    foreach (var cr in ceid.Reports.OrderBy(r => r.Sort))
                    {
                        var report = cr.Report;
                        if (report == null) continue;

                        // 复用已构建的 Report 节点（含其 VID 子节点）
                        if (reportNodes.TryGetValue(report.Id, out var node))
                        {
                            ceidNode.Children.Add(node);
                        }
                        else
                        {
                            ceidNode.Children.Add(BuildReportNode(report));
                        }
                    }

                    TreeNodes.Add(ceidNode);
                }

                // 未分配到任何 Report 的 VID
                var orphanVids = vids.Where(v => !assignedVidIds.Contains(v.Id)).ToList();
                if (orphanVids.Count > 0)
                {
                    var orphanNode = new SecsConfigTreeNode("未分配 VID", SecsTreeNodeKind.Vid);
                    foreach (var vid in orphanVids)
                        orphanNode.Children.Add(BuildVidNode(vid));
                    TreeNodes.Add(orphanNode);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("加载配置树失败：" + ex.Message);
            }
        }

        private static SecsConfigTreeNode BuildVidNode(SecsVidEntity vid)
        {
            return new SecsConfigTreeNode(
                $"{vid.VidType} {vid.VidId}：{vid.Name}",
                SecsTreeNodeKind.Vid,
                vid.Status == "0" ? "启用" : "停用",
                vid.Status == "0");
        }

        private static SecsConfigTreeNode BuildReportNode(SecsReportEntity report)
        {
            var node = new SecsConfigTreeNode(
                $"RPTID {report.ReportId}：{report.Name}",
                SecsTreeNodeKind.Report,
                report.Status == "0" ? "启用" : "停用",
                report.Status == "0");

            foreach (var rv in report.Vids.OrderBy(v => v.Sort))
            {
                var vid = rv.Vid;
                if (vid == null) continue;
                node.Children.Add(BuildVidNode(vid));
            }
            return node;
        }
    }
}
